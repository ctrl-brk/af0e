namespace QslDueWatcher;

internal static partial class Logging
{
    [LoggerMessage(Level = LogLevel.Information,
        Message = "Checking QSL deadlines due on {Today}; include overdue: {IncludeOverdue}")]
    public static partial void Starting(this ILogger logger, DateOnly today, bool includeOverdue);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "No new QSL reminders to send ({EligibleCount} eligible events)")]
    public static partial void NoReminders(this ILogger logger, int eligibleCount);

    [LoggerMessage(Level = LogLevel.Information,
        Message = "Sent {EventCount} QSL deadline reminders covering {ContactCount} contacts to {Recipient}")]
    public static partial void RemindersSent(this ILogger logger, int eventCount, int contactCount, string recipient);

    [LoggerMessage(Level = LogLevel.Information, Message = "QSL deadline check cancelled")]
    public static partial void Cancelled(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "QSL deadline check failed")]
    public static partial void Failed(this ILogger logger, Exception exception);
}
