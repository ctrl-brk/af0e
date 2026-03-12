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

        parkNum = NormalizeParkNumber(parkNum);

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

        parkNum = NormalizeParkNumber(parkNum);

        var res = await dbContext.PotaParks.Where(x => x.ParkNum == parkNum).FirstOrDefaultAsync();
        return res == null ? null : new PotaParkDetails(res);
    }

    public static async Task<List<PotaHuntingQsoSummary>> GetParkHuntingLog(string parkNum, HrdDbContext dbContext)
    {
        if (string.IsNullOrEmpty(parkNum))
            return [];

        parkNum = NormalizeParkNumber(parkNum);

        return await dbContext.PotaHunting
            .Include(l => l.Log)
            .ThenInclude(a => a.PotaContacts)
            .Include(p => p.Park)
            .Where(x => x.Park.ParkNum == parkNum)
            .OrderByDescending(x => x.Log.ColTimeOn)
            .Select(x => new PotaHuntingQsoSummary(x))
            .ToListAsync();
    }

    public static async Task<List<QsoSummary>> GetUnconfirmedContacts(HrdDbContext dbContext) =>
        await dbContext.Log
            .Where(x => EF.Functions.Like(x.ColComment, $"POTA%") && !EF.Functions.Like(x.ColComment, $"POTA act%"))
            .OrderBy(x => x.ColTimeOn)
            .Select(x => new QsoSummary(x, ExtractPotaReference(x.ColComment)))
            .ToListAsync();

    public static async Task<PotaActivityInfo> CheckActivity(string callSign, IPotaApiService potaApiService) =>
        await potaApiService.CheckActivityAsync(callSign: callSign);

    public static async Task<List<PotaActivityWithStats>> CheckActivity(string? band, string? mode, bool dups, IPotaApiService potaApiService, HrdDbContext dbContext)
    {
        var spots = await potaApiService.CheckActivityAsync(band, mode);
        var filteredSpots = dups ? spots : await FilterAlreadyLoggedSpots(spots, dbContext);

        if (filteredSpots.Count == 0)
            return [];

        // Get unique park numbers and call signs
        var parkNumbers = filteredSpots
            .Where(s => !string.IsNullOrEmpty(s.ParkNum))
            .Select(s => s.ParkNum!)
            .Distinct()
            .ToList();

        var callSigns = filteredSpots
            .Select(s => s.CallSign)
            .Distinct()
            .ToList();

        // Normalize park numbers
        var normalizedParkNums = parkNumbers
            .Select(NormalizeParkNumber)
            .ToList();

        // Query park contacts grouped by park, band, mode
        var parkStats = normalizedParkNums.Count > 0
            ? await dbContext.PotaHunting
                .Include(p => p.Park)
                .Include(l => l.Log)
                .Where(x => normalizedParkNums.Contains(x.Park.ParkNum))
                .GroupBy(x => new { x.Park.ParkNum, x.Log.ColBand, x.Log.ColMode })
                .Select(g => new
                {
                    ParkNum = g.Key.ParkNum,
                    Band = g.Key.ColBand ?? "",
                    Mode = g.Key.ColMode ?? "",
                    Count = g.Count()
                })
                .ToListAsync()
            : [];

        // Query total contacts per call sign
        var callSignStats = await dbContext.Log
            .Where(l => callSigns.Contains(l.ColCall))
            .GroupBy(l => l.ColCall)
            .Select(g => new
            {
                CallSign = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        // Create lookup dictionaries
        var parkStatsLookup = parkStats
            .GroupBy(s => s.ParkNum)
            .ToDictionary(
                g => g.Key,
                g => g.ToList()
            );

        var callSignStatsLookup = callSignStats
            .ToDictionary(s => s.CallSign, s => s.Count, StringComparer.OrdinalIgnoreCase);

        // Combine data
        return filteredSpots.Select(spot =>
        {
            var parkNum = spot.ParkNum ?? "";
            var normalizedParkNum = string.IsNullOrEmpty(parkNum) ? "" : NormalizeParkNumber(parkNum);

            var parkContactStats = parkStatsLookup.TryGetValue(normalizedParkNum, out var stats)
                ? stats.Select(s => new ParkContactStats
                {
                    Band = s.Band,
                    Mode = s.Mode,
                    Count = s.Count
                }).ToList()
                : [];

            var totalParkContacts = parkContactStats.Sum(s => s.Count);
            var totalCallSignContacts = callSignStatsLookup.GetValueOrDefault(spot.CallSign, 0);

            return new PotaActivityWithStats
            {
                Activity = spot,
                ParkContactsByBandMode = parkContactStats,
                TotalParkContacts = totalParkContacts,
                TotalCallSignContacts = totalCallSignContacts
            };
        }).ToList();
    }

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

    /// <summary>
    /// Normalizes a park number by adding "US-" prefix if it starts with a digit
    /// </summary>
    private static string NormalizeParkNumber(string parkNum) =>
        parkNum.Length > 0 && parkNum[0] >= '0' && parkNum[0] <= '9'
            ? $"US-{parkNum}"
            : parkNum;
}

static partial class PotaHandlers
{
    [GeneratedRegex(@"\b[A-Z]{2}-\d{4,5}\b")]
    private static partial Regex PotaReferenceRegex();
}
