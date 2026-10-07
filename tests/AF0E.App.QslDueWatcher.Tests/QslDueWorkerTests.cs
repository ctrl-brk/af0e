using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using QslDueWatcher;

namespace AF0E.App.QslDueWatcher.Tests;

public sealed class QslDueWorkerTests
{
    [Fact]
    public async Task PendingReminderIsEmailedThenMarkedSent()
    {
        var reminder = CreateReminder();
        var state = new TestStateStore();
        var sender = new TestEmailSender();
        using var lifetime = new TestApplicationLifetime();
        using var worker = CreateWorker(reminder, state, sender, lifetime);

        await RunWorkerAsync(worker, lifetime);

        Assert.Equal(1, sender.SendCount);
        Assert.Single(state.MarkedKeys);
        Assert.Equal(ReminderStateStore.CreateKey(reminder, "to@example.com"), state.MarkedKeys.Single());
    }

    [Fact]
    public async Task PreviouslySentReminderIsNotEmailedAgain()
    {
        var reminder = CreateReminder();
        var state = new TestStateStore();
        state.SentKeys.Add(ReminderStateStore.CreateKey(reminder, "to@example.com"));
        var sender = new TestEmailSender();
        using var lifetime = new TestApplicationLifetime();
        using var worker = CreateWorker(reminder, state, sender, lifetime);

        await RunWorkerAsync(worker, lifetime);

        Assert.Equal(0, sender.SendCount);
        Assert.Empty(state.MarkedKeys);
    }

    private static QslDueWorker CreateWorker(
        QslDueReminder reminder,
        TestStateStore state,
        TestEmailSender sender,
        TestApplicationLifetime lifetime)
    {
        var settings = Options.Create(new AppSettings
        {
            ConnectionString = "unused",
            TimeZoneId = "UTC",
            Email = new EmailSettings
            {
                From = "from@example.com",
                To = "to@example.com",
                Subject = "Reminder",
                Smtp = new SmtpSettings { Server = "unused" }
            }
        });

        return new QslDueWorker(
            new TestRepository(reminder),
            state,
            new TestFormatter(),
            sender,
            settings,
            new FixedTimeProvider(new DateTimeOffset(2026, 10, 6, 12, 0, 0, TimeSpan.Zero)),
            lifetime,
            NullLogger<QslDueWorker>.Instance);
    }

    private static async Task RunWorkerAsync(QslDueWorker worker, TestApplicationLifetime lifetime)
    {
        await worker.StartAsync(CancellationToken.None).ConfigureAwait(false);
        await lifetime.Stopped.Task.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
        await worker.StopAsync(CancellationToken.None).ConfigureAwait(false);
    }

    private static QslDueReminder CreateReminder() => new(
        10,
        "Test event",
        new DateTime(2026, 10, 10),
        null,
        null,
        [new QslDueContact(1, "AF0E", new DateTime(2026, 10, 1), "N")]);

    private sealed class TestRepository(QslDueReminder reminder) : IQslDueRepository
    {
        public Task<IReadOnlyList<QslDueReminder>> GetDueRemindersAsync(DateOnly today, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<QslDueReminder>>([reminder]);
    }

    private sealed class TestStateStore : IReminderStateStore
    {
        public HashSet<string> SentKeys { get; } = [];
        public List<string> MarkedKeys { get; } = [];

        public Task<HashSet<string>> LoadAsync(CancellationToken cancellationToken) =>
            Task.FromResult(SentKeys.ToHashSet(StringComparer.Ordinal));

        public Task MarkSentAsync(IEnumerable<string> reminderKeys, CancellationToken cancellationToken)
        {
            MarkedKeys.AddRange(reminderKeys);
            SentKeys.UnionWith(MarkedKeys);
            return Task.CompletedTask;
        }
    }

    private sealed class TestFormatter : IReminderEmailFormatter
    {
        public ReminderEmail Format(IReadOnlyList<QslDueReminder> reminders, DateOnly today) =>
            new("Subject", "Text", "<p>HTML</p>");
    }

    private sealed class TestEmailSender : IEmailSender
    {
        public int SendCount { get; private set; }

        public Task SendAsync(ReminderEmail email, CancellationToken cancellationToken)
        {
            SendCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class TestApplicationLifetime : IHostApplicationLifetime, IDisposable
    {
        private readonly CancellationTokenSource _started = new();
        private readonly CancellationTokenSource _stopping = new();
        private readonly CancellationTokenSource _stopped = new();

        public TaskCompletionSource Stopped { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public CancellationToken ApplicationStarted => _started.Token;
        public CancellationToken ApplicationStopping => _stopping.Token;
        public CancellationToken ApplicationStopped => _stopped.Token;

        public void StopApplication()
        {
            _stopping.Cancel();
            _stopped.Cancel();
            Stopped.TrySetResult();
        }

        public void Dispose()
        {
            _started.Dispose();
            _stopping.Dispose();
            _stopped.Dispose();
        }
    }
}


