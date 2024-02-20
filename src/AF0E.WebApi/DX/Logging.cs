namespace DX.Api;

// ReSharper disable once UnusedType.Global
public static partial class Logging
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Error")]
    public static partial void LogAppError(this ILogger logger, Exception ex);
}
