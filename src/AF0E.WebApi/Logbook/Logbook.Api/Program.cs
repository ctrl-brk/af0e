using System.Text.Json.Serialization;
using AF0E.DB;
using AF0E.Services.DxCluster;
using AF0E.Services.Pota;
using AF0E.Services.Qrz;
using Logbook.Api.Converters;
using Logbook.Api.Endpoints;
using Logbook.Api.Realtime;
using Logbook.Api.Security;
using Logbook.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using Serilog;

var isDevelopment = string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase)
    || string.Equals(Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase);

var bootstrapLoggerConfiguration = new LoggerConfiguration()
    .WriteTo.File("logs/logbook-api-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14);

if (isDevelopment)
    bootstrapLoggerConfiguration.WriteTo.Debug();

Log.Logger = bootstrapLoggerConfiguration.CreateBootstrapLogger();

try
{
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.Configuration
        .AddJsonFile("dxcluster.filters.json", optional: true, reloadOnChange: true)
        .AddJsonFile($"dxcluster.filters.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Conditional(_ => context.HostingEnvironment.IsDevelopment(), wt => wt.Debug()));

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi(options =>
    {
        options.AddDocumentTransformer((document, _, _) =>
        {
            // Add security scheme to the OpenAPI document
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "JWT Authorization header using the Bearer scheme. Enter your token in the text input below."
            };

            return Task.CompletedTask;
        });

        options.AddOperationTransformer((operation, context, _) =>
        {
            // Add security requirement to operations that require authorization
            var metadata = context.Description.ActionDescriptor.EndpointMetadata;
            var requiresAuth = metadata.OfType<IAuthorizeData>().Any();

            if (!requiresAuth)
                return Task.CompletedTask;

            operation.Security ??= [];
            operation.Security.Add(new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("bearer", context.Document)] = []
            });

            return Task.CompletedTask;
        });
    });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddHttpClient();
    builder.Services.AddScoped<IPotaApiService, PotaApiService>();
    builder.Services.AddSignalR();
    builder.Services.Configure<QrzSettings>(builder.Configuration.GetSection("QrzSettings"));
    builder.Services.Configure<ApiKeyAuthSettings>(builder.Configuration.GetSection("ApiKeyAuth"));
    builder.Services.AddDxCluster(builder.Configuration.GetSection("DxCluster"));
    builder.Services.AddSingleton<IDxccMatcher, DbDxccMatcher>();
    builder.Services.AddSingleton<IQrzService, QrzService>();
    builder.Services.AddSingleton<IDxClusterEventsPublisher, SignalRDxClusterEventsPublisher>();
    builder.Services.AddSingleton<DxClusterHubSessionManager>();
    builder.Services.AddScoped<ILogEventsPublisher, SignalRLogEventsPublisher>();

//builder.Services.AddDbContext<HrdDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("HrdLog")));
    builder.Services.AddScoped<HrdDbContext>(_ => new HrdDbContext(builder.Configuration.GetConnectionString("HrdLog")!));
    builder.Services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
    {
        options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.SerializerOptions.Converters.Add(new NullableNumericConverterFactory());
    });

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}/";
            options.Audience = builder.Configuration["Auth0:Audience"];
            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = "name",
                RoleClaimType = builder.Configuration["Auth0:RoleClaimType"],
            };
        })
        .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationDefaults.Scheme, _ => { });

    builder.Services.AddAuthorizationBuilder()
        .AddPolicy(Policies.AdminOnly, policy => policy
        .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme, ApiKeyAuthenticationDefaults.Scheme)
        .RequireRole(Roles.Admin));
    //.AddPolicy("CanWrite", policy => policy.RequireClaim("permissions", "write:data"));

    WebApplication app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseDefaultFiles();
    app.UseStaticFiles();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();

        app.MapScalarApiReference(options =>
        {
            options.WithTitle("Logbook API v1")
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);

            // Configure authentication - Scalar will display a UI to input the bearer token
            options.Authentication = new()
            {
                PreferredSecuritySchemes = ["bearer"]
            };
        });
    }
    else
    {
        app.UseHttpsRedirection();
    }

    app.UseExceptionHandler(exceptionApp =>
        exceptionApp.Run(async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status500InternalServerError;
            ctx.Response.ContentType = "application/json";

            var exceptionFeature = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            if (exceptionFeature is null) return;

            // In development, include the exception message and stack trace in the response
            object response = app.Environment.IsDevelopment()
                ? new { error = exceptionFeature.Error.Message, stackTrace = exceptionFeature.Error.StackTrace }
                : new { error = "An unexpected error occurred." };

            await ctx.Response.WriteAsJsonAsync(response);
        }));

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapGet("/health", () => Results.Ok(new { ok = true }));
    app.RegisterV1Endpoints();
    app.MapHub<LogbookHub>("/api/hubs/logbook");

    app.MapFallbackToFile("/index.html");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for integration tests
public partial class Program { }

