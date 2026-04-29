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

    // Enforce single instance
    const string mutexName = "RigCommander_SingleInstance";
    var instanceMutex = new Mutex(false, mutexName, out var createdNew);

    if (!createdNew)
    {
        MessageBox.Show("RigCommander is already running.", "Multiple Instances Not Allowed", MessageBoxButtons.OK, MessageBoxIcon.Information);
        Environment.Exit(0);
    }

    try
    {
        var richTextBoxSink = new RichTextBoxSink();
        var scriptActivityLog = new RichTextBoxActivityLog();

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Sink(richTextBoxSink, restrictedToMinimumLevel: LogEventLevel.Warning));

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
    var activationIdStore = new ActivationIdStore();

    scriptActivityLog.MinimumLevel = settings.Ui.ActivityLog.MinimumLevel;

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
    app.Lifetime.ApplicationStopping.Register(radio.Dispose);
    logger.LogInformation("Using profile '{Profile}' ({Kind})", activeProfile.Name, activeProfile.Kind);

    app.RegisterRadioEndpoints(radio, settings, logger);

    AdifApiForwarder? adifApiForwarder = null;

    if (settings.AdifUdp.Forwarding.Enabled)
    {
        try
        {
#pragma warning disable CA2000
            adifApiForwarder = new AdifApiForwarder(
                settings.LogbookApiUrl!,
                settings.AdifUdp.Forwarding,
                app.Services.GetRequiredService<ILogger<AdifApiForwarder>>(),
                scriptActivityLog,
                activationIdStore);
#pragma warning restore CA2000

            _ = adifApiForwarder.StartAsync(app.Lifetime.ApplicationStopping);
            app.Lifetime.ApplicationStopping.Register(adifApiForwarder.Dispose);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start ADIF API forwarder");
        }
    }

    if (settings.AdifUdp.Enabled)
    {
        try
        {
#pragma warning disable CA2000
            var adifUdpListener = new AdifUdpBroadcastListener(
                settings.AdifUdp,
                app.Services.GetRequiredService<ILogger<AdifUdpBroadcastListener>>(),
                scriptActivityLog,
                adifApiForwarder);
#pragma warning restore CA2000
            _ = adifUdpListener.StartAsync(app.Lifetime.ApplicationStopping);
            app.Lifetime.ApplicationStopping.Register(adifUdpListener.Dispose);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start ADIF UDP listener on port {Port}", settings.AdifUdp.Port);
        }
    }

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

    var serverUri = new Uri($"http://localhost:{settings.ListenPort}");

    // Start the HTTP server in the background
    _ = app.RunAsync(serverUri.ToString());

    // Start the WinForms shell
    using var mainForm = new MainForm(app, serverUri, settings, activationIdStore);
    var mainFormRef = new WeakReference<MainForm>(mainForm);

    void OnWarningOrErrorLogged(string message)
    {
        if (mainFormRef.TryGetTarget(out var form) && !form.IsDisposed)
            form.ShowErrorBalloon(message);
    }

    richTextBoxSink.WarningOrErrorEmitted += OnWarningOrErrorLogged;

    try
    {
        richTextBoxSink.Attach(mainForm.LogBox);
        scriptActivityLog.Attach(mainForm.ScriptLogBox);
        Application.Run(mainForm);
    }
    finally
    {
        richTextBoxSink.WarningOrErrorEmitted -= OnWarningOrErrorLogged;
    }
    }
    finally
    {
        instanceMutex.Dispose();
    }
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
