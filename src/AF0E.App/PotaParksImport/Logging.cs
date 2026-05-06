using System.Net;
using Microsoft.Extensions.Logging;

namespace PotaParksImport;

internal static partial class Logging
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Started")]
    public static partial void LogStarted(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Downloading park list from {url}")]
    public static partial void LogDownloading(this ILogger logger, Uri url);

    [LoggerMessage(Level = LogLevel.Information, Message = "Downloaded {count} parks")]
    public static partial void LogDownloadedCount(this ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Dry-run enabled: API download/deserialization only, DB operations are skipped")]
    public static partial void LogDryRunEnabled(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Dry-run completed. Would have inserted {count} rows and executed [dbo].[ImportPotaParks-US]")]
    public static partial void LogDryRunCompleted(this ILogger logger, int count);

    [LoggerMessage(Level = LogLevel.Information, Message = "Truncating dbo.PotaParksImport")]
    public static partial void LogTruncating(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Inserting rows into dbo.PotaParksImport")]
    public static partial void LogInserting(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Executing stored procedure [dbo].[ImportPotaParks-US]")]
    public static partial void LogRunningStoredProc(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Import completed. Downloaded {downloaded}; proc inserted {inserted}, updated {updated}, deactivated {deactivated}")]
    public static partial void LogCompleted(this ILogger logger, int downloaded, int inserted, int updated, int deactivated);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Stored procedure [dbo].[ImportPotaParks-US] did not return summary counts")]
    public static partial void LogProcSummaryMissing(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No rows returned by API")]
    public static partial void LogNoRows(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Invalid HTTP status code: {statusCode}")]
    public static partial void LogHttpError(this ILogger logger, HttpStatusCode statusCode);

    [LoggerMessage(Level = LogLevel.Error, Message = "Can not deserialize response from {url}")]
    public static partial void LogJsonError(this ILogger logger, Uri url);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error")]
    public static partial void LogException(this ILogger logger, Exception ex);
}
