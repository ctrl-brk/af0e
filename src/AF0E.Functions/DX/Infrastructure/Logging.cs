namespace AF0E.Functions.DX.Infrastructure;

// ReSharper disable once UnusedType.Global
// ReSharper disable once UnusedMember.Global
internal static partial class Logging
{
#if DEBUG
    [LoggerMessage(Level = LogLevel.Warning, Message = "[AF0E] DX dates differ for {callSign}\n\t       Begin                 End\n\t{existingBeginDate} | {existingEndDate}\t{existingSource}\n\t{mergingBeginDate} | {mergingEndDate}\t{mergingSource}")]
    public static partial void LogDxDifference(this ILogger logger, string callSign, DateTime existingBeginDate, DateTime existingEndDate, DxInfoSource existingSource, DateTime mergingBeginDate, DateTime mergingEndDate, DxInfoSource mergingSource);
#endif

    [LoggerMessage(Level = LogLevel.Information, Message = "[AF0E] {source}: Processed {recordsCount} records")]
    public static partial void LogSourceStats(this ILogger logger, DxInfoSource source, int recordsCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "[AF0E] No DX found")]
    public static partial void LogNoDx(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "[AF0E] {source}: Date Error - {date}")]
    public static partial void LogDateError(this ILogger logger, DxInfoSource source, string date);

    [LoggerMessage(Level = LogLevel.Error, Message = "[AF0E] error")]
    public static partial void LogAppError(this ILogger logger, Exception ex);

    [LoggerMessage(Level = LogLevel.Error, Message = "[AF0E] Entity upsert error: {entity}")]
    public static partial void LogUpsertError(this ILogger logger, Exception ex, DxInfoDto entity);
}
