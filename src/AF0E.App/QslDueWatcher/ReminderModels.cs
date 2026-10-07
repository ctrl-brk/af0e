namespace QslDueWatcher;

public sealed record QslDueReminder(
    int EventId,
    string EventName,
    DateTime DueDate,
    string? QslInfo,
    Uri? Url,
    IReadOnlyList<QslDueContact> Contacts);

public sealed record QslDueContact(
    int LogId,
    string Call,
    DateTime? QsoDate,
    string? QslSentStatus);

public sealed record ReminderEmail(string Subject, string PlainTextBody, string HtmlBody);
