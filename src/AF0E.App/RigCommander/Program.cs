using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using RigCommander;
using RigCommander.Endpoints;
using RigCommander.Services;
using Serilog;
using Serilog.Events;

#pragma warning disable CA1848 // Use the LoggerMessage delegates
#pragma warning disable CA1873

//default config in case appsettings fails
Log.Logger = new LoggerConfiguration()
    .WriteTo.File(
        path: "logs/rigcommander-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 14,
        shared: true,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] ({SourceContext}) {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

try
{
    ApplicationConfiguration.Initialize();

    var richTextBoxSink = new RichTextBoxSink();
    var scriptActivityLog = new RichTextBoxActivityLog();

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Sink(richTextBoxSink, restrictedToMinimumLevel: LogEventLevel.Warning));

    builder.Configuration
        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
        .AddEnvironmentVariables();

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    });

    builder.Services.Configure<JsonOptions>(o => o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase);
    builder.Services.AddSingleton<IValidateOptions<RigCommanderSettings>, RigCommanderSettingsValidator>();

    builder.Services
        .AddOptions<RigCommanderSettings>()
        .Bind(builder.Configuration.GetSection("RigCommander"))
        .ValidateOnStart();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            var feature = context.Features.Get<IExceptionHandlerFeature>();
            if (feature?.Error is not null)
            {
                var requestLogger = context.RequestServices.GetRequiredService<ILogger<Program>>();
                requestLogger.LogError(feature.Error, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            }

            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsJsonAsync(new
            {
                ok = false,
                error = "Internal server error"
            });
        });
    });

    var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
    var logger = loggerFactory.CreateLogger("RigCommander.App");
    var settings = app.Services.GetRequiredService<IOptions<RigCommanderSettings>>().Value;

    app.UseCors("AllowAll");

    app.MapGet("/health", () => Results.Ok(new { ok = true }));

    // -----------------------------------------------------------------------------
    // Radio selection
    // -----------------------------------------------------------------------------
    // ActiveProfile determines which rig to control; falls back to the first profile when unset.
    RadioProfileSettings activeProfile;

    try
    {
        activeProfile = RadioProfileResolver.Resolve(settings);
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Failed to resolve the active radio profile");
        throw new InvalidOperationException("RigCommander failed to resolve the active radio profile.", ex);
    }

#pragma warning disable CA2000
    var radio = RadioFactory.Create(activeProfile);
#pragma warning restore CA2000
    app.Lifetime.ApplicationStopping.Register(() => radio.Dispose());
    logger.LogInformation("Using profile '{Profile}' ({Kind})", activeProfile.Name, activeProfile.Kind);

    app.RegisterRadioEndpoints(radio, settings, logger);

    if (settings.Winkeyer?.Enabled is true)
    {
#pragma warning disable CA2000
        var winkeyer = new WinkeyerSerial(settings.Winkeyer.PortName, settings.Winkeyer.BaudRate, settings.Winkeyer.MinWpm, settings.Winkeyer.MaxWpm, TimeSpan.FromSeconds(settings.Winkeyer.IdleCloseSeconds), logger, scriptActivityLog);
#pragma warning restore CA2000

        app.RegisterWinkeyerEndpoints(winkeyer, radio, settings, logger);

        if (settings.Winkeyer.KeepHostOpen)
        {
            try
            {
                winkeyer.EnsureReady();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Winkeyer configured to keep port open but not found");
            }
        }

        app.Lifetime.ApplicationStopping.Register(winkeyer.Dispose);
    }

    // -----------------------------------------------------------------------------

    const string serverUrl = "http://localhost:5050";
    var serverUri = new Uri(serverUrl);

    // Start the HTTP server in the background
    _ = app.RunAsync(serverUrl);

    // Start the WinForms shell
    using var mainForm = new MainForm(app, serverUri, settings);
    richTextBoxSink.Attach(mainForm.LogBox);
    scriptActivityLog.Attach(mainForm.ScriptLogBox);
    Application.Run(mainForm);
}
catch (Exception ex)
{
    Log.Fatal(ex, "RigCommander terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
