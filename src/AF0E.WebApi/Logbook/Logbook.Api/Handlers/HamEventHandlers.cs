using AF0E.DB;
using Logbook.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Logbook.Api.Handlers;

public static class HamEventHandlers
{
    public static async Task<List<HamEventSummary>> GetEvents(DateTimeOffset? since, HrdDbContext dbContext, CancellationToken ct)
    {
        return await dbContext.HamEvents
            .AsNoTracking()
            .Where(x => x.StartDate == null || since == null || x.StartDate >= since)
            .OrderByDescending(x => x.StartDate)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.HamEventId)
            .Select(x => new HamEventSummary(x))
            .ToListAsync(ct);
    }

    public static async Task<List<HamEventSummary>> GetActiveEvents(HrdDbContext dbContext, CancellationToken ct)
    {
        var utcToday = DateTime.UtcNow.Date;

        return await dbContext.HamEvents
            .AsNoTracking()
            .Where(x => (x.StartDate == null || x.StartDate <= utcToday) && (x.EndDate == null || x.EndDate >= utcToday))
            .OrderByDescending(x => x.StartDate)
            .ThenBy(x => x.Name)
            .ThenBy(x => x.HamEventId)
            .Select(x => new HamEventSummary(x))
            .ToListAsync(ct);
    }
}
