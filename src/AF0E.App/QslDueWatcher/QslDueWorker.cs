using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace QslDueWatcher;

public sealed class QslDueWorker(
    IQslDueRepository repository,
    IReminderStateStore stateStore,
    IReminderEmailFormatter formatter,
    IEmailSender emailSender,
    IOptions<AppSettings> options,
    TimeProvider timeProvider,
    IHostApplicationLifetime applicationLifetime,
    ILogger<QslDueWorker> logger) : BackgroundService
{
    private readonly AppSettings _settings = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(_settings.TimeZoneId);
            var localNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone);
            var today = DateOnly.FromDateTime(localNow.DateTime);

            logger.Starting(today, _settings.IncludeOverdue);

            var reminders = await repository.GetDueRemindersAsync(today, stoppingToken);
            var sentKeys = await stateStore.LoadAsync(stoppingToken);
            var pending = reminders
                .Select(reminder => new
                {
                    Reminder = reminder,
                    Key = ReminderStateStore.CreateKey(reminder, _settings.Email.To)
                })
                .Where(x => !sentKeys.Contains(x.Key))
                .ToList();

            if (pending.Count == 0)
            {
                logger.NoReminders(reminders.Count);
                return;
            }

            var email = formatter.Format([.. pending.Select(x => x.Reminder)], today);
            await emailSender.SendAsync(email, stoppingToken);
            await stateStore.MarkSentAsync(pending.Select(x => x.Key), stoppingToken);

            if (logger.IsEnabled(LogLevel.Information))
                logger.RemindersSent(pending.Count, pending.Sum(x => x.Reminder.Contacts.Count), _settings.Email.To);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.Cancelled();
        }
        catch (Exception ex)
        {
            Environment.ExitCode = 1;
            logger.Failed(ex);
        }
        finally
        {
            applicationLifetime.StopApplication();
        }
    }
}
