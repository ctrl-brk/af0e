using AF0E.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace QslDueWatcher;

public interface IQslDueRepository
{
    Task<IReadOnlyList<QslDueReminder>> GetDueRemindersAsync(DateOnly today, CancellationToken cancellationToken);
}

public sealed class QslDueRepository(IOptions<AppSettings> options) : IQslDueRepository
{
    private readonly AppSettings _settings = options.Value;

    public async Task<IReadOnlyList<QslDueReminder>> GetDueRemindersAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var rangeStart = today.ToDateTime(TimeOnly.MinValue);
        var rangeEndExclusive = today.AddDays(1).ToDateTime(TimeOnly.MinValue);

        await using var dbContext = new HrdDbContext(_settings.ConnectionString);

        var query = dbContext.HamEvents
            .Where(x => x.QslDueDate != null && x.QslDueDate < rangeEndExclusive);

        if (!_settings.IncludeOverdue)
            query = query.Where(x => x.QslDueDate >= rangeStart);

        var events = await query
            .Where(x => x.HamEventContacts.Any(c =>
                c.Log.ColQslsdate == null &&
                (string.IsNullOrEmpty(c.Log.ColQslSent) || c.Log.ColQslSent == "N" || c.Log.ColQslSent == "Q")))
            .Select(x => new
            {
                x.HamEventId,
                x.Name,
                DueDate = x.QslDueDate!.Value,
                x.QslInfo,
                x.Url,
                Contacts = x.HamEventContacts
                    .Where(c => c.Log.ColQslsdate == null &&
                                (string.IsNullOrEmpty(c.Log.ColQslSent) || c.Log.ColQslSent == "N" || c.Log.ColQslSent == "Q"))
                    .OrderBy(c => c.Log.ColTimeOn)
                    .Select(c => new QslDueContact(c.LogId, c.Log.ColCall, c.Log.ColTimeOn, c.Log.ColQslSent))
                    .ToList()
            })
            .OrderBy(x => x.DueDate)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return events
            .Select(x => new QslDueReminder(x.HamEventId, x.Name, x.DueDate, x.QslInfo, x.Url, x.Contacts))
            .ToList();
    }
}
