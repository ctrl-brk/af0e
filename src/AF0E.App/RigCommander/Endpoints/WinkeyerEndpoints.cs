using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using RigCommander.Abstractions;
using RigCommander.Contracts;

namespace RigCommander.Endpoints;

[SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates")]
public static class WinkeyerEndpoints
{
    private static bool _splitOk;

    public static void RegisterWinkeyerEndpoints(this WebApplication app, WinkeyerSerial? winkeyer, IRadio radio, RigCommanderSettings settings, ILogger logger)
    {
        app.MapPost("/winkeyer/send", (WinkeyerSendRequest req) =>
            {
                if (winkeyer is null)
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
                    var split = false;
                    var sent = false;

                    if (req.RigControl is true)
                    {
                        split = radio.WithConnection(radio.GetStatus).SplitOn;
                        if (!split)
                            _splitOk = false;
                    }

                    if (!split || (split && _splitOk))
                    {
                        if (req.Wpm is not null)
                            winkeyer.SetWpm(req.Wpm.Value);

                        winkeyer.SendScript(req.Text, repeat, repeatDelaySeconds);
                        sent = true;
                    }

                    if (split)
                        _splitOk = true;

                    var status = winkeyer.GetStatus();

                    return Results.Ok(new
                    {
                        ok = true,
                        wpm = req.Wpm,
                        repeat,
                        repeatDelaySeconds,
                        hostWpm = status.HostWpm,
                        speedPotRaw = status.SpeedPotRaw,
                        speedPotWpm = status.SpeedPotWpm,
                        effectiveWpm = status.EffectiveWpm,
                        busy = status.Busy,
                        wait = status.Wait,
                        xoff = status.Xoff,
                        split,
                        sent,
                    });
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    return Results.BadRequest(new
                    {
                        ok = false,
                        error = ex.Message
                    });
                }
                catch (ArgumentException ex)
                {
                    return Results.BadRequest(new
                    {
                        ok = false,
                        error = ex.Message
                    });
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
                if (winkeyer is null)
                    return Results.Json(new { ok = false, error = "Winkeyer is disabled." }, statusCode: StatusCodes.Status503ServiceUnavailable);

                try
                {
                    winkeyer.SetWpm(req.Wpm);
                    var status = winkeyer.GetStatus();

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
                if (winkeyer is null)
                    return Results.Json(new { ok = false, error = "Winkeyer is disabled." }, statusCode: StatusCodes.Status503ServiceUnavailable);

                try
                {
                    winkeyer.Abort();
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

        app.MapGet("/winkeyer/health", () =>
            {
                if (winkeyer is null)
                    return Results.Json(new { ok = false, error = "Winkeyer is disabled." }, statusCode: StatusCodes.Status503ServiceUnavailable);

                try
                {
                    winkeyer.EnsureReady();

                    var status = winkeyer.GetStatus();

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
                if (winkeyer is null)
                    return Results.Json(new { ok = false, error = "Winkeyer is disabled." }, statusCode: StatusCodes.Status503ServiceUnavailable);

                try
                {
                    var status = winkeyer.GetStatus();

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

    }
}

