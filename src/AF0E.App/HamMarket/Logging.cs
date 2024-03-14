namespace HamMarket;

public static partial class Logging
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Started")]
    public static partial void LogStarted(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Stopping")]
    public static partial void LogStopping(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error sending email. {errorMessage}")]
    public static partial void LogSendEmailError(this ILogger logger, string errorMessage);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error")]
    public static partial void LogException(this ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Saving file")]
    public static partial void LogSavingFile(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Sending {emailProvider} email to {emailTo}")]
    public static partial void LogSendingSmtpEmail(this ILogger logger, string emailProvider, string emailTo);
}
