using System.Net.Mime;
using RigCommander.Contracts;

namespace RigCommander.Endpoints;

public static class WinkeyerEndpoints
{
    public static void RegisterWinkeyerEndpoints(this WebApplication app, WinkeyerSerial? winkey, RigCommanderSettings settings, ILogger logger)
    {
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
                    if (req.Wpm is not null)
                        winkey.SetWpm(req.Wpm.Value);

                    winkey.SendScript(req.Text, repeat, repeatDelaySeconds);

                    var status = winkey.GetStatus();

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
                        xoff = status.Xoff
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

    }
}

