using System.Net;
using Microsoft.Extensions.Logging;

namespace QrzLookup;

internal static partial class Logging
{
    [LoggerMessage(Level = LogLevel.Information, Message = "{logId} - {call} {lng}:{lat} ({src}) [{cnt}/{total}]")]
    public static partial void LogCurrentCall(this ILogger logger, int logId, string call, decimal? Lng, decimal? Lat, string? src, int cnt, int total);

    [LoggerMessage(Level = LogLevel.Information, Message = "Started")]
    public static partial void LogStarted(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Updated {cnt} records")]
    public static partial void LogUpdated(this ILogger logger, int cnt);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error {statusCode}. {errorMessage}, {requestMessage}")]
    public static partial void LogAppError(this ILogger logger, HttpStatusCode statusCode, string? errorMessage, HttpRequestMessage? requestMessage);

    [LoggerMessage(Level = LogLevel.Error, Message = "Configuration error: {errorMessage}")]
    public static partial void LogConfigurationError(this ILogger logger, string? errorMessage);

    [LoggerMessage(Level = LogLevel.Error, Message = "QRZ Error: {msg}")]
    public static partial void LogInvalidQrzResponse(this ILogger logger, string msg);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Not found: {call}")]
    public static partial void LogCallNotFound(this ILogger logger, string call);

    [LoggerMessage(Level = LogLevel.Error, Message = "Invalid XML: {xml}")]
    public static partial void LogInvalidXml(this ILogger logger, string xml, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "{error}\nMessage: {message}")]
    public static partial void LogQrzApiError(this ILogger logger, string? error, string? message);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error")]
    public static partial void LogException(this ILogger logger, Exception ex);
}
