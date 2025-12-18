using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AF0E.DB;
using AF0E.DB.Models;
using AF0E.Common.Pota;

namespace PotaHuntingLookup;

internal sealed class HostedService(ILogger<HostedService> logger, IHostApplicationLifetime appLifeTime, IOptions<AppSettings> settings) : IHostedService, IDisposable
{
    private Task? _task;
    private CancellationTokenSource? _cts;
    private HttpClient? _httpClient;
    private HrdDbContext? _dbContext;
    private Dictionary<string, PotaPark> _potaParks = null!;
    private readonly string[] _args = Environment.GetCommandLineArgs(); //args here are normal. args[0] is the app file name
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [SuppressMessage("Performance", "CA1849:Call async methods when in an async method")]
    public Task StartAsync(CancellationToken ct)
    {
        string token;

        try
        {
            token = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, settings.Value.AuthTokenFileName));
        }
        catch (Exception e)
        {
            logger.LogException(e);
            return Task.CompletedTask;
        }

        _httpClient = new HttpClient { BaseAddress = settings.Value.PotaApiUrl };
        _httpClient.DefaultRequestHeaders.Add("Authorization", token);
        _dbContext = new HrdDbContext(settings.Value.ConnectionString);
        // Create a linked token, so we can trigger cancellation outside of this token's cancellation
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        _task = LookupContactsAsync(_cts.Token);

        // If the task is completed, then return it; otherwise it's running
        return _task.IsCompleted ? _task : Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken _)
    {
        return Task.CompletedTask;
    }

    private async Task LookupContactsAsync(CancellationToken ct)
    {
        const int PageSize = 100;
        var pageNum = 1;
        var contacts = new List<PotaLogEntry>();
        int cnt;

        var startDate = DateTime.Parse(_args[1]);
        var endDate = DateTime.Parse(_args[2]);

        logger.LogAppStarted(_args[1], _args[2]);

        do
        {
            var url = $"/{settings.Value.LogbookRoute}?hunterOnly=1&page={pageNum}&size={PageSize}&startDate={startDate:yyyy-MM-dd}&endDate={endDate:yyyy-MM-dd}";
            var response = await _httpClient!.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogHttpError(response.StatusCode);
                appLifeTime.StopApplication();
                return;
            }

            var res = await response.Content.ReadAsStringAsync(ct);
            var result = JsonSerializer.Deserialize<PotaLogResponse>(res, _jsonOptions);
            if (result == null)
            {
                logger.LogJsonError(url);
                break;
            }
            contacts.AddRange(result.Entries);
            cnt = result.Count;
        } while (pageNum++ * PageSize < cnt);

        var changesMade = false;

        if (contacts.Count > 0)
        {
            logger.LogProcessing(contacts.Count);

            _potaParks = await _dbContext!.PotaParks.ToDictionaryAsync(x => x.ParkNum, x => x, ct);
            changesMade = await UpdateLog(contacts, ct);
        }
        else
            logger.LogNoQsos();

        if (changesMade)
            await DisplayNotLinkedQsos(startDate, endDate, ct);

        appLifeTime.StopApplication();
    }

    [SuppressMessage("ReSharper.DPA", "DPA0007: Large number of DB records")]
    private async Task<bool> UpdateLog(List<PotaLogEntry> potaLog, CancellationToken ct)
    {
        var notFoundCalls = new List<PotaLogEntry>();
        var bandMismatchCalls = new List<PotaLogEntry>();
        // log comes sorted descending by time
        var startDate = potaLog.Last().QsoDateTime.AddMinutes(-15);
        var endDate = potaLog.First().QsoDateTime.AddMinutes(15);
        var matchBand = _args.Length > 3;

        // select existing contacts for a given date range
        var hrdLog = await _dbContext!.Log
            .AsTracking()
            .Include(x => x.PotaHunting)
            .Where(x => x.ColTimeOn >= startDate && x.ColTimeOn < endDate)
            .ToListAsync(ct);

        foreach (var potaQso in potaLog)
        {
            var found = false;
            var bandMismatch = false;

            // try to match by call, band and date, adding 5 min to each side of the date window, since time might not be in sync on both sides,
            // ideally there should be only one match, but who knows, maybe there's a dupe or something
            foreach (var q in hrdLog.Where(hrdQso =>
                         hrdQso.ColCall.Trim() == potaQso.StationCallsign &&
                         (!matchBand || hrdQso.ColBand!.Equals(potaQso.Band, StringComparison.OrdinalIgnoreCase)) &&
                         hrdQso.ColTimeOn > potaQso.QsoDateTime.AddMinutes(-5) &&
                         hrdQso.ColTimeOn < potaQso.QsoDateTime.AddMinutes(5)))
            {
                found = true;

                var park = await LoadPark(potaQso.Reference, ct);

                if (park == null) //this should never happen
                {
                    logger.LogParkNotFound(potaQso.Reference);
                    continue;
                }

                // n-fers and already logged
                if (q.PotaHunting.Any(x => x.ParkId == park.ParkId))  //already synced
                    continue;

                if (!q.ColBand!.Equals(potaQso.Band, StringComparison.OrdinalIgnoreCase))
                {
                    bandMismatch = true;
                    bandMismatchCalls.Add(potaQso);
                }

                var newRecord = new PotaHunting
                {
                    LogId = q.ColPrimaryKey,
                    ParkId = park.ParkId,
                    Park = park.ParkId > 0 ? null! : park,
                    P2P = potaQso.P2pMatch.HasValue,
#pragma warning disable CA1308
                    BandReported = bandMismatch ? potaQso.Band.ToLowerInvariant() : null,
#pragma warning restore CA1308
                };

                q.PotaHunting.Add(newRecord); //no tracking
                _dbContext.PotaHunting.Add(newRecord);

                // strip POTA* from comment, if present
                if (!Helpers.TryStripPotaCode(q.ColComment, out var match, out var result))
                    continue;

                logger.LogStripedComment(match!, q.ColComment!, q.ColCall, q.ColPrimaryKey);
                q.ColComment = result;
            }

            if (!found)
                notFoundCalls.Add(potaQso);
        }

        var color = Console.ForegroundColor;

        if (notFoundCalls.Count > 0)
        {
            var uniqueCalls = notFoundCalls.DistinctBy(x => x.StationCallsign).ToList();
            var calls = string.Join(',', uniqueCalls.Select(x => x.StationCallsign));

            var msg = $"The following {uniqueCalls.Count} call(s) were not found, probably time mismatch - compare the time below with HRD:";
#if (!DEBUG)
            logger.LogWarning(msg);
#endif
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            foreach (var q in notFoundCalls)
            {
                msg = $"    {q.QsoDateTime:yy-MM-dd HH:mm}   {q.StationCallsign}\t{q.Band}\t{q.Mode}\t{q.Reference}{(q.P2pMatch == 1 ? "\tP2P" : "")}";
#if (!DEBUG)
                logger.LogWarning(msg);
#endif
                Console.WriteLine(msg);
            }

            logger.LogCallsNotFound(calls);
        }

        if (bandMismatchCalls.Count > 0)
        {
            var uniqueCalls = bandMismatchCalls.DistinctBy(x => x.StationCallsign).ToList();
            var calls = string.Join(',', uniqueCalls.Select(x => x.StationCallsign));

            var msg = $"\n\nThe following {uniqueCalls.Count} call(s) were found, but on the other band:";
#if (!DEBUG)
            logger.LogWarning(msg);
#endif
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(msg);
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            foreach (var q in bandMismatchCalls)
            {
                msg = $"    {q.QsoDateTime:yy-MM-dd HH:mm}   {q.StationCallsign}\t{q.Band}\t{q.Mode}\t{q.Reference}{(q.P2pMatch == 1 ? "\tP2P" : "")}";
#if (!DEBUG)
                logger.LogWarning(msg);
#endif
                Console.WriteLine(msg);
            }

            logger.LogCallsBandMismatch(calls);
        }

        if (_dbContext.ChangeTracker.HasChanges())
        {
            if (notFoundCalls.Count > 0 || bandMismatchCalls.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("Save changes (Y/n)? ");
                Console.ForegroundColor = color;

                if (Console.ReadKey().KeyChar != 'Y')
                    return false;
            }

            try
            {
                Console.WriteLine("\n\nSaving...");
                await _dbContext.SaveChangesAsync(CancellationToken.None);
                return true;
            }
            catch (Exception e)
            {
                logger.LogException(e);
            }
        }
        else
            logger.LogNoNewQsos();

        return false;
    }

    private async Task DisplayNotLinkedQsos(DateTime startDate, DateTime endDate, CancellationToken ct)
    {
        var qsos = await _dbContext!.Log
            .Where(x =>
                x.ColTimeOn >= startDate && x.ColTimeOn < endDate
                && EF.Functions.Like(x.ColComment, "POTA%")
                && !EF.Functions.Like(x.ColComment, "POTA activation%"))
            .ToListAsync(ct);

        if (qsos.Count == 0)
            return;

        var color = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n\nThe following {qsos.Count} call(s) still have a POTA* comment:");
        Console.ForegroundColor = ConsoleColor.DarkYellow;
        foreach (var q in qsos)
        {
            Console.WriteLine($"    {q.ColTimeOn:yy-MM-dd HH:mm}   {q.ColCall}\t{q.ColBand}\t{q.ColMode}\t{q.ColComment}");
        }
        Console.ForegroundColor = color;
    }

    private async Task<PotaPark?> LoadPark(string parkNum, CancellationToken ct)
    {
        if (_potaParks.TryGetValue(parkNum, out var park))
            return park;

        var url = $"/{settings.Value.ParkInfoRoute}/{parkNum}";
        var response = await _httpClient!.GetAsync(url, ct);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogHttpError(response.StatusCode);
            appLifeTime.StopApplication();
            return null;
        }

        var res = await response.Content.ReadAsStringAsync(ct);
        var parkRes = JsonSerializer.Deserialize<PotaParkInfo>(res, _jsonOptions);
        if (parkRes == null)
        {
            logger.LogJsonError(url);
            return null;
        }

        park = new PotaPark
        {
            ParkNum = parkNum,
            ParkName = parkRes.Name,
            Lat = parkRes.Latitude,
            Long = parkRes.Longitude,
            Grid = parkRes.Grid6,
            Location = parkRes.LocationDesc,
            Country = parkRes.ReferencePrefix,
            Active = parkRes.Active == 1,
        };

        if (park.Active) //no stats returned for inactive parks :(
        {
            url = $"/{settings.Value.ParkStatsRoute}/{parkNum}";
            response = await _httpClient!.GetAsync(url, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogHttpError(response.StatusCode);
                appLifeTime.StopApplication();
                return null;
            }

            res = await response.Content.ReadAsStringAsync(ct);
            try
            {
                var statsRes = JsonSerializer.Deserialize<PotaParkStats>(res, _jsonOptions);
                park.TotalActivationCount = statsRes!.Activations;
                park.TotalQsoCount = statsRes.Contacts;
            }
            catch
            {
                logger.LogJsonError(url);
            }
        }

        _potaParks.Add(park.ParkNum, park);
        logger.LogNewPark(park.ParkNum, park.ParkName);
        return park;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _cts?.Dispose();
        _httpClient?.Dispose();
        _dbContext?.Dispose();
    }
}
