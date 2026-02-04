using System.Text.RegularExpressions;
using AF0E.DB;
using AF0E.Services.Pota;
using AF0E.Services.Pota.Models;
using Logbook.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Logbook.Api.Handlers;

public static partial class PotaHandlers
{
    public static async Task<List<PotaActivationSummary>> GetActivations(HrdDbContext dbContext) =>
        await dbContext.PotaActivations
            .Include(x => x.Park)
            .Include(x => x.PotaContacts)
            .ThenInclude(l => l.Log)
            .OrderByDescending(x => x.StartDate)
            .Select(x => new PotaActivationSummary(x))
            .ToListAsync();

    public static async Task<PotaActivationDetails?> GetActivation(int id, HrdDbContext dbContext)
    {
        var res = await dbContext.PotaActivations
            .Include(x => x.Park)
            .Include(x => x.PotaContacts)
            .ThenInclude(l => l.Log)
            .FirstOrDefaultAsync(x => x.ActivationId == id);

        return res == null ? null : new PotaActivationDetails(res);
    }

    public static async Task<List<PotaActivationQsoSummary>> GetActivationLog(int id, HrdDbContext dbContext) =>
        await dbContext.PotaContacts
            .Where(x => x.ActivationId == id)
            .Include(x => x.Log)
            .Select(x => new PotaActivationQsoSummary(x))
            .ToListAsync();

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

    public static async Task<List<QsoSummary>> GetUnconfirmedContacts(HrdDbContext dbContext) =>
        await dbContext.Log
            .Where(x => EF.Functions.Like(x.ColComment, $"POTA%") && !EF.Functions.Like(x.ColComment, $"POTA act%"))
            .OrderBy(x => x.ColTimeOn)
            .Select(x => new QsoSummary(x, ExtractPotaReference(x.ColComment)))
            .ToListAsync();

    public static async Task<List<PotaActivityInfo>> CheckActivity(IPotaApiService potaApiService) =>
        await potaApiService.CheckActivityAsync();

    public static async Task<List<PotaActivityInfo>> CheckActivity(string? band, string? mode, IPotaApiService potaApiService, HrdDbContext dbContext)
    {
        var spots = await potaApiService.CheckActivityAsync(band, mode);
        return await FilterAlreadyLoggedSpots(spots, dbContext);
    }

    public static async Task<PotaActivityInfo> CheckActivity(string callSign, IPotaApiService potaApiService) =>
        await potaApiService.CheckActivityAsync(callSign: callSign);

    private static async Task<List<PotaActivityInfo>> FilterAlreadyLoggedSpots(List<PotaActivityInfo> spots, HrdDbContext dbContext)
    {
        if (spots.Count == 0)
            return spots;

        // Get all call signs from spots
        var callSigns = spots.Select(s => s.CallSign).Distinct().ToList();
        
        // Get today's date range
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var todayStart = today.ToDateTime(TimeOnly.MinValue);
        var todayEnd = today.ToDateTime(TimeOnly.MaxValue);

        // Query all today's contacts for these call signs
        var todaysContacts = await dbContext.Log
            .Where(l => callSigns.Contains(l.ColCall) 
                && l.ColTimeOn >= todayStart 
                && l.ColTimeOn <= todayEnd
                && l.ColComment != null 
                && l.ColComment.StartsWith("POTA "))
            .Select(l => new
            {
                l.ColCall,
                l.ColMode,
                l.ColBand,
                l.ColComment,
                l.ColTimeOn
            })
            .ToListAsync();

        // Filter out spots that match existing contacts
        return [.. spots.Where(spot =>
        {
            if (!spot.Active || spot.LastSpotTime == null)
                return true;

            var spotDate = DateOnly.FromDateTime(spot.LastSpotTime.Value.UtcDateTime);
            
            return !todaysContacts.Any(contact =>
            {
                if (!string.Equals(contact.ColCall, spot.CallSign, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.Equals(contact.ColMode, spot.Mode, StringComparison.OrdinalIgnoreCase))
                    return false;

                if (!string.Equals(contact.ColBand, spot.Band, StringComparison.OrdinalIgnoreCase))
                    return false;

                var contactDate = DateOnly.FromDateTime(contact.ColTimeOn!.Value);
                if (contactDate != spotDate)
                    return false;

                // Extract park reference from comment (format: "POTA XXX ...")
                var parkRef = ExtractPotaReference(contact.ColComment);
                return string.Equals(parkRef, spot.ParkNum, StringComparison.OrdinalIgnoreCase);
            });
        })];
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
