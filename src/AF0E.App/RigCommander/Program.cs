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

// -----------------------------------------------------------------------------
// Endpoints
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

logger.LogInformation("Listening on port {Port}", settings.ListenPort);
app.Run($"http://0.0.0.0:{settings.ListenPort}");
