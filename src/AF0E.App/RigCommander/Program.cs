using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using RigCommander;
using RigCommander.Endpoints;

ApplicationConfiguration.Initialize();

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddLogging();

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

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        if (feature?.Error is not null)
            Console.WriteLine($"[HTTP] Unhandled exception: {feature.Error}");

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
    var winkeyer = new WinkeyerSerial(settings.Winkeyer.PortName, settings.Winkeyer.BaudRate, settings.Winkeyer.MinWpm, settings.Winkeyer.MaxWpm, TimeSpan.FromSeconds(settings.Winkeyer.IdleCloseSeconds), logger);

    app.RegisterWinkeyerEndpoints(winkeyer, radio, settings, logger);

    if (settings.Winkeyer.KeepHostOpen)
        winkeyer.EnsureReady();

    app.Lifetime.ApplicationStopping.Register(() => winkeyer.Dispose());
}

// -----------------------------------------------------------------------------

const string serverUrl = "http://localhost:5050";

// Start the HTTP server in the background
_ = app.RunAsync(serverUrl);

// Start the WinForms shell
using var mainForm = new MainForm(app, serverUrl, settings);
Application.Run(mainForm);
