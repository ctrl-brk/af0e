using System.Net;
using Microsoft.Extensions.Logging;

namespace HrdDxFilter;

public static partial class Logging
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Started")]
    public static partial void LogStarted(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "DX filter set to {value}")]
    public static partial void LogDxFilterValue(this ILogger logger, string value);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No DX received")]
    public static partial void LogNoDx(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error {statusCode}. {errorMessage}, {requestMessage}")]
    public static partial void LogDxApiError(this ILogger logger, HttpStatusCode statusCode, string? errorMessage, HttpRequestMessage? requestMessage );

    [LoggerMessage(Level = LogLevel.Error, Message = "Error")]
    public static partial void LogException(this ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Web filters file not found or not configured: {path}")]
    public static partial void LogWebFilterFileMissing(this ILogger logger, string? path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Could not locate DxCluster.Filters array in {path}")]
    public static partial void LogWebFilterArrayMissing(this ILogger logger, string path);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Filter '{title}' not found in {path}")]
    public static partial void LogWebFilterNotFound(this ILogger logger, string title, string path);
}
