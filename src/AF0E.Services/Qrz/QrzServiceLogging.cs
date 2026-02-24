using Microsoft.Extensions.Logging;

namespace AF0E.Services.Qrz;

internal static partial class QrzServiceLogging
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Invalid XML response from QRZ: {Response}")]
    public static partial void LogInvalidXmlResponse(this ILogger logger, string response, Exception ex);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid QRZ response: {Error}")]
    public static partial void LogInvalidQrzResponse(this ILogger logger, string error);

    [LoggerMessage(Level = LogLevel.Error, Message = "QRZ XML username or password is missing")]
    public static partial void LogMissingCredentials(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Invalid QRZ session response: {Response}")]
    public static partial void LogInvalidSessionResponse(this ILogger logger, string response);

    [LoggerMessage(Level = LogLevel.Error, Message = "QRZ API error: {Error}, Message: {Message}")]
    public static partial void LogQrzApiError(this ILogger logger, string? error, string? message);

    [LoggerMessage(Level = LogLevel.Error, Message = "Configuration error: {errorMessage}")]
    public static partial void LogConfigurationError(this ILogger logger, string? errorMessage);

}
