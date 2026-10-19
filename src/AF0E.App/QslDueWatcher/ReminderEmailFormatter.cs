using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.Options;

namespace QslDueWatcher;

public interface IReminderEmailFormatter
{
    ReminderEmail Format(IReadOnlyList<QslDueReminder> reminders, DateOnly today);
}

public sealed class ReminderEmailFormatter(IOptions<AppSettings> options) : IReminderEmailFormatter
{
    private readonly AppSettings _settings = options.Value;

    public ReminderEmail Format(IReadOnlyList<QslDueReminder> reminders, DateOnly today)
    {
        var subject = $"{_settings.Email.Subject} ({reminders.Count})";
        var text = new StringBuilder();
        var html = new StringBuilder("""
            <!doctype html>
            <html lang="en"><head><meta charset="utf-8"><style>
            body{font-family:Arial,sans-serif;color:#222} section{margin:0 0 1.5rem}
            h2{margin-bottom:.25rem} table{border-collapse:collapse} th,td{border:1px solid #bbb;padding:.35rem .5rem;text-align:left}
            .overdue{color:#b00020;font-weight:bold}.due{font-weight:bold}
            </style></head><body><h1>QSL deadline reminder</h1>
            """);

        foreach (var reminder in reminders)
        {
            var dueDate = DateOnly.FromDateTime(reminder.DueDate);
            var daysRemaining = dueDate.DayNumber - today.DayNumber;
            var status = FormatDueStatus(daysRemaining);

            text.AppendLine(CultureInfo.InvariantCulture, $"{reminder.EventName} — due {dueDate:yyyy-MM-dd} ({status})");
            if (reminder.Url is not null)
                text.AppendLine(reminder.Url.ToString());
            if (!string.IsNullOrWhiteSpace(reminder.QslInfo))
                text.AppendLine(reminder.QslInfo);
            text.AppendLine("Outstanding contacts:");

            html.Append("<section><h2>").Append(WebUtility.HtmlEncode(reminder.EventName)).Append("</h2>");
            html.Append("<div class=\"").Append(daysRemaining < 0 ? "overdue" : "due").Append("\">Due ")
                .Append(dueDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(" (")
                .Append(WebUtility.HtmlEncode(status)).AppendLine(")</div>");

            if (reminder.Url is not null)
            {
                var encodedUrl = WebUtility.HtmlEncode(reminder.Url.ToString());
                html.Append("<div><a href=\"").Append(encodedUrl).Append("\">Event website</a></div>");
            }

            if (!string.IsNullOrWhiteSpace(reminder.QslInfo))
                html.Append("<p>").Append(WebUtility.HtmlEncode(reminder.QslInfo).Replace("\n", "<br>", StringComparison.Ordinal)).AppendLine("</p>");

            html.AppendLine("<table><thead><tr><th>Call</th><th>QSO date</th><th>QSL status</th></tr></thead><tbody>");
            foreach (var contact in reminder.Contacts)
            {
                var qsoDate = contact.QsoDate?.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture) ?? "Unknown";
                var qslStatus = string.IsNullOrWhiteSpace(contact.QslSentStatus) ? "N" : contact.QslSentStatus;

                text.AppendLine(CultureInfo.InvariantCulture, $"  {contact.Call,-12} {qsoDate}  QSL sent: {qslStatus}");
                html.Append("<tr><td>").Append(WebUtility.HtmlEncode(contact.Call)).Append("</td><td>")
                    .Append(WebUtility.HtmlEncode(qsoDate)).Append("</td><td>")
                    .Append(WebUtility.HtmlEncode(qslStatus)).AppendLine("</td></tr>");
            }

            text.AppendLine();
            html.AppendLine("</tbody></table></section>");
        }

        html.AppendLine("</body></html>");
        return new ReminderEmail(subject, text.ToString(), html.ToString());
    }

    internal static string FormatDueStatus(int daysRemaining) => daysRemaining switch
    {
        < 0 => $"overdue by {-daysRemaining} day{(-daysRemaining == 1 ? string.Empty : "s")}",
        0 => "due today",
        1 => "1 day remaining",
        _ => $"{daysRemaining} days remaining"
    };
}
