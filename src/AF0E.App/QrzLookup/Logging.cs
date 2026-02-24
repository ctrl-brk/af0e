using Microsoft.Extensions.Logging;

namespace QrzLookup;

internal static partial class Logging
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Started")]
    public static partial void LogStarted(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Not found: {call}")]
    public static partial void LogCallNotFound(this ILogger logger, string call);

    [LoggerMessage(Level = LogLevel.Information, Message = "{logId} - {call} {lng}:{lat} ({src}) [{cnt}/{total}]")]
    public static partial void LogCurrentCall(this ILogger logger, int logId, string call, decimal? Lng, decimal? Lat, string? src, int cnt, int total);

    [LoggerMessage(Level = LogLevel.Information, Message = "Updated {cnt} records")]
    public static partial void LogUpdated(this ILogger logger, int cnt);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error")]
    public static partial void LogException(this ILogger logger, Exception ex);
}
