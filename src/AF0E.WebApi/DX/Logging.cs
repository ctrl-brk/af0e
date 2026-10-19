namespace DX.Api;

// ReSharper disable once UnusedType.Global
#pragma warning disable CA1724 //name conflicst with MS namespace
public static partial class Logging
{
    [LoggerMessage(Level = LogLevel.Error, Message = "Error")]
    public static partial void LogAppError(this ILogger logger, Exception ex);
}
