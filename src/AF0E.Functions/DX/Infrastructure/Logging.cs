namespace AF0E.Functions.DX.Infrastructure;

// ReSharper disable once UnusedType.Global
// ReSharper disable once UnusedMember.Global
public static partial class Logging
{
#if DEBUG
    [LoggerMessage(Level = LogLevel.Warning, Message = "DX dates differ for {callSign}\n\t       Begin                 End\n\t{existingBeginDate} | {existingEndDate}\t{existingSource}\n\t{mergingBeginDate} | {mergingEndDate}\t{mergingSource}")]
    public static partial void LogDxDifference(this ILogger logger, string callSign, DateTime existingBeginDate, DateTime existingEndDate, DxInfoSource existingSource, DateTime mergingBeginDate, DateTime mergingEndDate, DxInfoSource mergingSource);
#endif

    [LoggerMessage(Level = LogLevel.Information, Message = "{source}: Processed {recordsCount} records")]
    public static partial void LogSourceStats(this ILogger logger, DxInfoSource source, int recordsCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No DX found")]
    public static partial void LogNoDx(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error")]
    public static partial void LogAppError(this ILogger logger, Exception ex);
}
