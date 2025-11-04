using System.Text.RegularExpressions;
using AF0E.DB;
using Logbook.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Logbook.Api.Handlers;

public static partial class PotaHandlers
{
    public static async Task<List<PotaActivationSummary>> GetActivations(HrdDbContext dbContext)
    {
        return await dbContext.PotaActivations
            .Include(x => x.Park)
            .Include(x => x.PotaContacts)
            .ThenInclude(l => l.Log)
            .OrderByDescending(x => x.StartDate)
            .Select(x => new PotaActivationSummary(x))
            .ToListAsync();
    }

    public static async Task<PotaActivationDetails?> GetActivation(int id, HrdDbContext dbContext)
    {
        var res = await dbContext.PotaActivations
            .Include(x => x.Park)
            .Include(x => x.PotaContacts)
            .ThenInclude(l => l.Log)
            .FirstOrDefaultAsync(x => x.ActivationId == id);

        return res == null ? null : new PotaActivationDetails(res);
    }

    public static async Task<List<PotaActivationQsoSummary>> GetActivationLog(int id, HrdDbContext dbContext)
    {
        return await dbContext.PotaContacts
            .Where(x => x.ActivationId == id)
            .Include(x => x.Log)
            .Select(x => new PotaActivationQsoSummary(x))
            .ToListAsync();
    }

    public static async Task<List<PotaParkDetails>> GetParks(string parkNum, int maxResults, HrdDbContext dbContext)
    {
        if (string.IsNullOrEmpty(parkNum))
            return [];

        if (parkNum[0] >= '0' && parkNum[0] <= '9')
            parkNum = $"US-{parkNum}";

        return await dbContext.PotaParks
            .Where(x => x.Active && x.ParkNum.StartsWith(parkNum) || EF.Functions.Like(x.ParkName, $"%{parkNum}%"))
            .OrderBy(x => x.ParkNum)
            .Take(maxResults)
            .Select(x => new PotaParkDetails(x))
            .ToListAsync();
    }

    public static async Task<PotaParkDetails?> GetPark(string parkNum, HrdDbContext dbContext)
    {
        if (string.IsNullOrEmpty(parkNum))
            return null;

        if (parkNum[0] >= '0' && parkNum[0] <= '9')
            parkNum = $"US-{parkNum}";

        var res = await dbContext.PotaParks.Where(x => x.ParkNum == parkNum).FirstOrDefaultAsync();
        return res == null ? null : new PotaParkDetails(res);
    }

    public static async Task<List<PotaHuntingQsoSummary>> GetParkHuntingLog(string parkNum, HrdDbContext dbContext)
    {
        if (string.IsNullOrEmpty(parkNum))
            return [];

        if (parkNum[0] >= '0' && parkNum[0] <= '9')
            parkNum = $"US-{parkNum}";

        return await dbContext.PotaHunting
            .Include(l => l.Log)
            .ThenInclude(a => a.PotaContacts)
            .Include(p => p.Park)
            .Where(x => x.Park.ParkNum == parkNum)
            .OrderByDescending(x => x.Log.ColTimeOn)
            .Select( x => new PotaHuntingQsoSummary(x))
            .ToListAsync();
    }

    public static async Task<List<QsoSummary>> GetUnconfirmedContacts(HrdDbContext dbContext)
    {
        return await dbContext.Log
            .Where(x => EF.Functions.Like(x.ColComment, $"POTA%") && !EF.Functions.Like(x.ColComment, $"POTA act%"))
            .OrderBy(x => x.ColTimeOn)
            .Select(x => new QsoSummary(x, ExtractPotaReference(x.ColComment)))
            .ToListAsync();
    }

    private static string ExtractPotaReference(string? comment)
    {
        if (string.IsNullOrEmpty(comment))
            return string.Empty;

        var match = PotaReferenceRegex().Match(comment);
        return match.Success ? match.Value : string.Empty;
    }
}

static partial class PotaHandlers
{
    [GeneratedRegex(@"\b[A-Z]{2}-\d{4,5}\b")]
    private static partial Regex PotaReferenceRegex();
}
