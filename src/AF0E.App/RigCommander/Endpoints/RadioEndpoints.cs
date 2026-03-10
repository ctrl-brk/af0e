using System.Net.Mime;
using RigCommander.Abstractions;
using RigCommander.Contracts;

namespace RigCommander.Endpoints;

public static class RadioEndpoints
{
    public static void RegisterRadioEndpoints(this WebApplication app, IRadio radio, RigCommanderSettings settings, ILogger logger)
    {
        app.MapPost("/radio/status", (SetStatusRequest req) =>
            {
                var statusDelayMs = Math.Max(0, settings.StatusDelayMs);

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

        app.MapPost("/radio/frequency", (SetFrequencyRequest req) =>
            {
                var statusDelayMs = Math.Max(0, settings.StatusDelayMs);

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

        app.MapGet("/radio/status", () =>
            {
                try
                {
                    var status = radio.WithConnection(radio.GetStatus);
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

        app.MapGet("/radio/frequency", () =>
            {
                try
                {
                    var hz = radio.WithConnection(radio.GetFrequency);
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
                    var status = radio.WithConnection(radio.GetStatus);
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
    }
}
