using AF0E.DB;
using Logbook.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Logbook.Api.Handlers;

public static class GridTrackerHandlers
{
    public static async Task<List<GridTrackerLookup>> GetGridTrackerLog(string call, HrdDbContext dbContext)
    {
        return await dbContext.Log
            .Include(x => x.PotaHunting)
            .ThenInclude(h => h.Park)
            .Where(x => EF.Functions.Like(x.ColCall, call))
            .OrderByDescending(x => x.ColTimeOn)
            .Select(x => new GridTrackerLookup(x))
            .ToListAsync();
    }

    public static async Task<List<GridTrackerParkStats>> GetGridTrackerParkStats(string parkNum, HrdDbContext dbContext)
    {
        return await dbContext.PotaHunting
            .Include(x => x.Park)
            .Include(x => x.Log)
            .Where(x => x.Park.ParkNum == parkNum)
            .GroupBy(x => x.Log.ColBand)
            .OrderByDescending(x => x.Key)
            .Select(x => new GridTrackerParkStats(x.Key!, x.Count()))
            .ToListAsync();
    }
}
