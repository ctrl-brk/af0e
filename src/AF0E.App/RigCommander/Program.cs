using System.Net.Mime;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using RigCommander;
using RigCommander.Contracts;

#pragma warning disable CA1848 //use the LoggerMessage delegates

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Services.AddLogging();

// CORS: allow all (be careful if you ever expose beyond localhost/LAN)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy => policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

// Keep JSON predictable
builder.Services.Configure<JsonOptions>(o => o.SerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddSingleton<IValidateOptions<RigCommanderSettings>, RigCommanderSettingsValidator>();

builder.Services
    .AddOptions<RigCommanderSettings>()
    .Bind(builder.Configuration.GetSection("RigCommander"))
    .ValidateOnStart();

var app = builder.Build();
var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
var logger = loggerFactory.CreateLogger("RigCommander.App");

// Global exception handler: never terminate the server for request exceptions.
// Converts uncaught exceptions into JSON 500 responses.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        if (feature?.Error is not null)
            logger.LogError(feature.Error, "[HTTP] Unhandled exception");

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new { ok = false, error = "Internal server error" });
    });
});

app.UseStatusCodePages(async ctx =>
{
    // Ensure non-success codes still return JSON
    ctx.HttpContext.Response.ContentType = "application/json";
    await ctx.HttpContext.Response.WriteAsJsonAsync(new
    {
        ok = false,
        status = ctx.HttpContext.Response.StatusCode
    });
});

app.UseCors("AllowAll");

app.MapGet("/health", () => Results.Ok(new { ok = true }));

// -----------------------------------------------------------------------------
// Radio selection
// -----------------------------------------------------------------------------
// ActiveProfile determines which rig to control; falls back to the first profile when unset.
var settings = app.Services.GetRequiredService<IOptions<RigCommanderSettings>>().Value;
var statusDelayMs = Math.Max(0, settings.StatusDelayMs);

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


WinkeyerSerial? winkey = null;
if (settings.Winkeyer?.Enabled is true)
{
    winkey = new WinkeyerSerial(settings.Winkeyer.PortName, settings.Winkeyer.BaudRate, settings.Winkeyer.MinWpm, settings.Winkeyer.MaxWpm, TimeSpan.FromSeconds(settings.Winkeyer.IdleCloseSeconds), logger);

    if (settings.Winkeyer.KeepHostOpen)
        winkey.EnsureReady();

    app.Lifetime.ApplicationStopping.Register(() => winkey.Dispose());
}

// -----------------------------------------------------------------------------
// Radio Endpoints
// -----------------------------------------------------------------------------

app.MapPost("/radio/frequency", (SetFrequencyRequest req) =>
{
    if (req.FrequencyHz is <= 0 or > 3_000_000_000L)
        return Results.BadRequest(new { ok = false, error = "FrequencyHz must be a positive Hz value in a reasonable range." });

    try
    {
        var status = radio.WithConnection(() =>
        {
            radio.SetFrequency(req.FrequencyHz);
            if (statusDelayMs > 0)
                Thread.Sleep(statusDelayMs);
            return radio.GetStatus();
        });

        return Results.Ok(new
        {
            ok = true,
            applied = new { frequencyHz = req.FrequencyHz },
            current = new
            {
                frequencyHz = status.FrequencyHz,
                mode = status.Mode,
                filter = status.Filter,
                data = status.DataModeOn
            }
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[/radio/frequency POST] Radio unavailable");
        return Results.Json(new { ok = false, error = "Radio unavailable (CAT/CI-V communication failure)" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.Accepts<SetFrequencyRequest>(MediaTypeNames.Application.Json)
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status503ServiceUnavailable);

app.MapPost("/radio/mode", (SetModeRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.Mode))
        return Results.BadRequest(new { ok = false, error = "Mode is required." });

    try
    {
        var status = radio.WithConnection(() =>
        {
            radio.SetMode(req.Mode);
            return radio.GetStatus();
        });

        return Results.Ok(new
        {
            ok = true,
            applied = new { mode = req.Mode },
            current = new
            {
                frequencyHz = status.FrequencyHz,
                mode = status.Mode,
                filter = status.Filter,
                data = status.DataModeOn
            }
        });
    }
    catch (ArgumentException ex)
    {
        logger.LogWarning(ex, "[/radio/mode POST] Invalid mode");
        return Results.BadRequest(new { ok = false, error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[/radio/mode POST] Radio unavailable");
        return Results.Json(new { ok = false, error = "Radio unavailable (CAT/CI-V communication failure)" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.Accepts<SetModeRequest>(MediaTypeNames.Application.Json)
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status503ServiceUnavailable);

app.MapPost("/radio/status", (SetStatusRequest req) =>
{
    if (req.FrequencyHz is null && string.IsNullOrWhiteSpace(req.Mode))
        return Results.BadRequest(new { ok = false, error = "Provide FrequencyHz and/or Mode." });

    if (req.FrequencyHz is <= 0 or > 3_000_000_000L)
        return Results.BadRequest(new { ok = false, error = "FrequencyHz must be a positive Hz value in a reasonable range." });

    try
    {
        var status = radio.WithConnection(() =>
        {
            if (!string.IsNullOrWhiteSpace(req.Mode))
                radio.SetMode(req.Mode!);

            if (req.FrequencyHz is null)
                return radio.GetStatus();

            radio.SetFrequency(req.FrequencyHz.Value);
            if (statusDelayMs > 0)
                Thread.Sleep(statusDelayMs);

            return radio.GetStatus();
        });

        return Results.Ok(new
        {
            ok = true,
            applied = new { req.FrequencyHz, req.Mode },
            current = new
            {
                frequencyHz = status.FrequencyHz,
                mode = status.Mode,
                filter = status.Filter,
                data = status.DataModeOn
            }
        });
    }
    catch (ArgumentException ex)
    {
        logger.LogWarning(ex, "[/radio/status POST] Invalid payload");
        return Results.BadRequest(new { ok = false, error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[/radio/status POST] Radio unavailable");
        return Results.Json(new { ok = false, error = "Radio unavailable (CAT/CI-V communication failure)" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.Accepts<SetStatusRequest>(MediaTypeNames.Application.Json)
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status503ServiceUnavailable);

app.MapGet("/radio/frequency", () =>
{
    try
    {
        var hz = radio.WithConnection(() => radio.GetFrequency());
        return Results.Ok(new { ok = true, frequencyHz = hz });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[/radio/frequency GET] Radio unavailable");
        return Results.Json(new { ok = false, error = "Radio unavailable (CAT/CI-V communication failure)" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status503ServiceUnavailable);

app.MapGet("/radio/mode", () =>
{
    try
    {
        var status = radio.WithConnection(() => radio.GetStatus());
        return Results.Ok(new { ok = true, mode = status.Mode, filter = status.Filter, data = status.DataModeOn });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[/radio/mode GET] Radio unavailable");
        return Results.Json(new { ok = false, error = "Radio unavailable (CAT/CI-V communication failure)" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status503ServiceUnavailable);

app.MapGet("/radio/status", () =>
{
    try
    {
        var status = radio.WithConnection(() => radio.GetStatus());
        return Results.Ok(new { ok = true, frequencyHz = status.FrequencyHz, mode = status.Mode, filter = status.Filter, data = status.DataModeOn });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[/radio/status GET] Radio unavailable");
        return Results.Json(new { ok = false, error = "Radio unavailable (CAT/CI-V communication failure)" }, statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status503ServiceUnavailable);

// -----------------------------------------------------------------------------
// Winkeyer Endpoints
// -----------------------------------------------------------------------------

app.MapGet("/winkeyer/health", () =>
    {
        if (winkey is null)
            return Results.Json(new { ok = false, error = "Winkeyer is disabled." }, statusCode: StatusCodes.Status503ServiceUnavailable);

        try
        {
            winkey.EnsureReady();

            var status = winkey.GetStatus();

            return Results.Ok(new
            {
                ok = true,
                portOpen = status.PortOpen,
                hostOpen = status.HostOpen,
                revision = status.Revision,
                lastActivityUtc = status.LastActivityUtc,
                idleSeconds = status.IdleSeconds,
                busy = status.Busy,
                wait = status.Wait,
                xoff = status.Xoff,
                speedPotRaw = status.SpeedPotRaw,
                speedPotWpm = status.SpeedPotWpm,
                hostWpm = status.HostWpm,
                effectiveWpm = status.EffectiveWpm,
                minWpm = status.MinWpm,
                maxWpm = status.MaxWpm,
                wpmRange = status.WpmRange
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[/winkeyer/health] Winkeyer unavailable");
            return Results.Json(new { ok = false, error = "Winkeyer unavailable" }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    })
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status503ServiceUnavailable);

app.MapGet("/winkeyer/status", () =>
    {
        if (winkey is null)
            return Results.Json(new { ok = false, error = "Winkeyer is disabled." }, statusCode: StatusCodes.Status503ServiceUnavailable);

        try
        {
            var status = winkey.GetStatus();

            return Results.Ok(new
            {
                ok = true,
                portOpen = status.PortOpen,
                hostOpen = status.HostOpen,
                revision = status.Revision,
                lastActivityUtc = status.LastActivityUtc,
                idleSeconds = status.IdleSeconds,
                busy = status.Busy,
                wait = status.Wait,
                xoff = status.Xoff,
                speedPotRaw = status.SpeedPotRaw,
                speedPotWpm = status.SpeedPotWpm,
                hostWpm = status.HostWpm,
                effectiveWpm = status.EffectiveWpm,
                minWpm = status.MinWpm,
                maxWpm = status.MaxWpm,
                wpmRange = status.WpmRange
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[/winkeyer/status] Failed to read Winkeyer status");
            return Results.Json(new { ok = false, error = "Failed to read Winkeyer status" }, statusCode: StatusCodes.Status500InternalServerError);
        }
    })
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status503ServiceUnavailable)
    .Produces(StatusCodes.Status500InternalServerError);

app.MapPost("/winkeyer/send", (WinkeyerSendRequest req) =>
    {
        if (winkey is null)
            return Results.Json(new { ok = false, error = "Winkeyer is disabled." }, statusCode: StatusCodes.Status503ServiceUnavailable);

        if (string.IsNullOrWhiteSpace(req.Text))
            return Results.BadRequest(new { ok = false, error = "Text is required." });

        var repeat = req.Repeat ?? 1;
        if (repeat is < 1 or > 100)
            return Results.BadRequest(new { ok = false, error = "Repeat must be between 1 and 100." });

        var repeatDelaySeconds = req.RepeatDelaySeconds ?? 0;
        if (repeatDelaySeconds is < 0 or > 3600)
            return Results.BadRequest(new { ok = false, error = "RepeatDelaySeconds must be between 0 and 3600." });

        try
        {
            winkey.SendScript(req.Text, repeat, repeatDelaySeconds);
            var status = winkey.GetStatus();

            return Results.Ok(new
            {
                ok = true,
                repeat,
                repeatDelaySeconds,
                hostWpm = status.HostWpm,
                speedPotRaw = status.SpeedPotRaw,
                speedPotWpm = status.SpeedPotWpm,
                effectiveWpm = status.EffectiveWpm,
                busy = status.Busy,
                wait = status.Wait,
                xoff = status.Xoff
            });
        }
        catch (ArgumentException ex)
        {
            return Results.BadRequest(new { ok = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[/winkeyer/send] Failed");
            return Results.Json(new { ok = false, error = "Failed to send Winkeyer text." }, statusCode: StatusCodes.Status500InternalServerError);
        }
    })
    .Accepts<WinkeyerSendRequest>(MediaTypeNames.Application.Json)
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status500InternalServerError)
    .Produces(StatusCodes.Status503ServiceUnavailable);

// wpm = 0 puts the keyer into the pot-following mode
// hostWpm = null means the pot is controlling speed
// effectiveWpm is the one to show in UI
app.MapPost("/winkeyer/wpm", (WinkeyerSetWpmRequest req) =>
    {
        if (winkey is null)
            return Results.Json(new { ok = false, error = "Winkeyer is disabled." }, statusCode: StatusCodes.Status503ServiceUnavailable);

        try
        {
            winkey.SetWpm(req.Wpm);
            var status = winkey.GetStatus();

            return Results.Ok(new
            {
                ok = true,
                hostWpm = status.HostWpm,
                speedPotRaw = status.SpeedPotRaw,
                speedPotWpm = status.SpeedPotWpm,
                effectiveWpm = status.EffectiveWpm,
                minWpm = status.MinWpm,
                maxWpm = status.MaxWpm,
                wpmRange = status.WpmRange
            });
        }
        catch (ArgumentOutOfRangeException ex)
        {
            return Results.BadRequest(new { ok = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[/winkeyer/wpm] Failed");
            return Results.Json(new { ok = false, error = "Failed to set Winkeyer WPM." }, statusCode: StatusCodes.Status500InternalServerError);
        }
    })
    .Accepts<WinkeyerSetWpmRequest>(MediaTypeNames.Application.Json)
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status400BadRequest)
    .Produces(StatusCodes.Status500InternalServerError)
    .Produces(StatusCodes.Status503ServiceUnavailable);

app.MapGet("/winkeyer/abort", () =>
    {
        if (winkey is null)
            return Results.Json(new { ok = false, error = "Winkeyer is disabled." }, statusCode: StatusCodes.Status503ServiceUnavailable);

        try
        {
            winkey.Abort();
            return Results.Ok(new { ok = true });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[/winkeyer/abort] Winkeyer unavailable");
            return Results.Json(new { ok = false, error = "Winkeyer unavailable (serial failure)" }, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    })
    .Produces(StatusCodes.Status200OK)
    .Produces(StatusCodes.Status503ServiceUnavailable);

// ------------------------------

logger.LogInformation("Listening on port {Port}", settings.ListenPort);
app.Run($"http://0.0.0.0:{settings.ListenPort}");
