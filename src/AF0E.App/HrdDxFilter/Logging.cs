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
}
