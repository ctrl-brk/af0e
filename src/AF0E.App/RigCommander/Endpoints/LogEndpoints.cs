using System.Diagnostics.CodeAnalysis;
using System.Net.Mime;
using System.Text;
using AF0E.Common.Utils;
using RigCommander.Contracts;

#pragma warning disable CA1873

namespace RigCommander.Endpoints;

[SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates")]
public static class LogEndpoints
{
    private static readonly SemaphoreSlim _writeLock = new(1, 1);
    private static readonly UTF8Encoding _utf8NoBom = new(false);

    public static void RegisterLogEndpoints(this WebApplication app, RigCommanderSettings settings, ILogger logger)
    {
        app.MapPost("/log/adif", async (SaveAdifRequest req, CancellationToken ct) =>
            {
                if (string.IsNullOrWhiteSpace(req.Adif))
                    return Results.BadRequest(new { ok = false, error = "Adif is required." });

                var normalizedAdif = NormalizeAdif(req.Adif);
                var records = AdifParser.Parse(normalizedAdif);
                if (records.Count == 0)
                    return Results.BadRequest(new { ok = false, error = "Adif must contain at least one ADIF record." });

                var filePath = ResolveFilePath(settings.OfflineLog.FilePath);
                var directory = Path.GetDirectoryName(filePath);

                try
                {
                    if (!string.IsNullOrWhiteSpace(directory))
                        Directory.CreateDirectory(directory);

                    await _writeLock.WaitAsync(ct);
                    try
                    {
                        await File.AppendAllTextAsync(filePath, normalizedAdif + Environment.NewLine, _utf8NoBom, ct);
                    }
                    finally
                    {
                        _writeLock.Release();
                    }

                    logger.LogInformation("[/log/adif POST] Saved {Count} ADIF record(s) to {Path}", records.Count, filePath);

                    return Results.Ok(new
                    {
                        ok = true,
                        recordsSaved = records.Count,
                        path = filePath
                    });
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    logger.LogWarning("[/log/adif POST] Request cancelled while writing ADIF to {Path}", filePath);
                    return Results.StatusCode(499);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "[/log/adif POST] Failed to save ADIF to {Path}", filePath);
                    return Results.Json(new { ok = false, error = "Failed to save ADIF to the local offline log." }, statusCode: StatusCodes.Status500InternalServerError);
                }
            })
            .Accepts<SaveAdifRequest>(MediaTypeNames.Application.Json)
            .Produces(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status500InternalServerError);
    }

    private static string NormalizeAdif(string adif)
    {
        var normalized = adif.Trim();

        if (!normalized.Contains("<EOR>", StringComparison.OrdinalIgnoreCase))
            normalized += " <EOR>";

        return normalized;
    }

    private static string ResolveFilePath(string configuredPath)
        => Path.IsPathRooted(configuredPath)
            ? Path.GetFullPath(configuredPath)
            : Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, configuredPath));
}


