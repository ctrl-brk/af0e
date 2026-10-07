using Microsoft.Extensions.Options;
using QslDueWatcher;

namespace AF0E.App.QslDueWatcher.Tests;

public sealed class ReminderTests
{
    [Fact]
    public void FormatterIncludesDeadlineContactsAndEscapesHtml()
    {
        var formatter = new ReminderEmailFormatter(CreateOptions());
        var reminder = new QslDueReminder(
            12,
            "Test <Event>",
            new DateTime(2026, 10, 10),
            "Mail to A&B",
            new Uri("https://example.com/event?a=1&b=2"),
            [new QslDueContact(7, "K0ABC<", new DateTime(2026, 10, 1, 12, 30, 0), "N")]);

        var email = formatter.Format([reminder], new DateOnly(2026, 10, 6));

        Assert.Equal("QSL deadline reminder (1)", email.Subject);
        Assert.Contains("4 days remaining", email.PlainTextBody, StringComparison.Ordinal);
        Assert.Contains("K0ABC<", email.PlainTextBody, StringComparison.Ordinal);
        Assert.Contains("Test &lt;Event&gt;", email.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("Mail to A&amp;B", email.HtmlBody, StringComparison.Ordinal);
        Assert.Contains("K0ABC&lt;", email.HtmlBody, StringComparison.Ordinal);
        Assert.DoesNotContain("Test <Event>", email.HtmlBody, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("2026-10-05", "overdue by 1 day")]
    [InlineData("2026-10-04", "overdue by 2 days")]
    [InlineData("2026-10-06", "due today")]
    [InlineData("2026-10-07", "1 day remaining")]
    public void FormatterDescribesDeadlineBoundary(string dueDate, string expected)
    {
        var formatter = new ReminderEmailFormatter(CreateOptions());
        var reminder = new QslDueReminder(
            1,
            "Boundary event",
            DateTime.Parse(dueDate, System.Globalization.CultureInfo.InvariantCulture),
            null,
            null,
            [new QslDueContact(1, "AF0E", null, null)]);

        var email = formatter.Format([reminder], new DateOnly(2026, 10, 6));

        Assert.Contains(expected, email.PlainTextBody, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StateStorePersistsKeysAcrossInstances()
    {
        var stateFile = Path.Combine(Path.GetTempPath(), $"qsl-due-watcher-{Guid.NewGuid():N}.json");
        var options = CreateOptions(stateFile);

        try
        {
            var firstStore = new ReminderStateStore(options);
            await firstStore.MarkSentAsync(["KEY-1", "KEY-2"], CancellationToken.None);

            var secondStore = new ReminderStateStore(options);
            var restored = await secondStore.LoadAsync(CancellationToken.None);

            Assert.Equal(2, restored.Count);
            Assert.Contains("KEY-1", restored);
            Assert.Contains("KEY-2", restored);
        }
        finally
        {
            File.Delete(stateFile);
            File.Delete(stateFile + ".tmp");
        }
    }

    [Fact]
    public void ReminderKeyChangesWithDeadlineOrRecipient()
    {
        var original = CreateReminder(new DateTime(2026, 10, 10));
        var changedDeadline = CreateReminder(new DateTime(2026, 10, 11));

        var key = ReminderStateStore.CreateKey(original, "one@example.com");

        Assert.Equal(key, ReminderStateStore.CreateKey(original, " ONE@example.com "));
        Assert.NotEqual(key, ReminderStateStore.CreateKey(changedDeadline, "one@example.com"));
        Assert.NotEqual(key, ReminderStateStore.CreateKey(original, "two@example.com"));
    }

    private static QslDueReminder CreateReminder(DateTime dueDate) =>
        new(1, "Test", dueDate, null, null, [new QslDueContact(1, "AF0E", null, "N")]);

    private static IOptions<AppSettings> CreateOptions(string stateFile = "test-state.json") => Options.Create(new AppSettings
    {
        ConnectionString = "Server=(local);Database=test",
        StateFile = stateFile,
        Email = new EmailSettings
        {
            From = "from@example.com",
            To = "to@example.com",
            Subject = "QSL deadline reminder",
            Smtp = new SmtpSettings { Server = "smtp.example.com" }
        }
    });
}


