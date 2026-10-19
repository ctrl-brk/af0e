using System.ComponentModel.DataAnnotations;

namespace QslDueWatcher;

public sealed class AppSettings
{
    [Required]
    public string ConnectionString { get; init; } = string.Empty;

    public bool IncludeOverdue { get; init; } = true;

    [Required]
    public string TimeZoneId { get; init; } = "Mountain Standard Time";

    [Required]
    public string StateFile { get; init; } = "qsl-due-watcher-state.json";

    [Required]
    public EmailSettings Email { get; init; } = new();
}

public sealed class EmailSettings
{
    [Required]
    public string From { get; init; } = string.Empty;

    public string FromName { get; init; } = "QSL Due Watcher";

    [Required]
    public string To { get; init; } = string.Empty;

    [Required]
    public string Subject { get; init; } = "QSL deadline reminder";

    [Required]
    public SmtpSettings Smtp { get; init; } = new();
}

public sealed class SmtpSettings
{
    [Required]
    public string Server { get; init; } = string.Empty;

    [Range(1, 65535)]
    public int Port { get; init; } = 587;

    public string? User { get; init; }
    public string? Password { get; init; }
}
