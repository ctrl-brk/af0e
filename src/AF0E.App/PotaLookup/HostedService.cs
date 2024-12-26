using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AF0E.DB;
using AF0E.DB.Models;
using PotaLookup.Models;

namespace PotaLookup;

internal sealed class HostedService(ILogger<HostedService> logger, IHostApplicationLifetime appLifeTime, IOptions<AppSettings> settings) : IHostedService, IDisposable
{
    private Task? _task;
    private CancellationTokenSource? _cts;
    private HttpClient? _httpClient;
    private HrdDbContext? _dbContext;
    private readonly List<PotaPark> _newParks = [];
    private readonly string[] _args = Environment.GetCommandLineArgs(); //args here are normal. args[0] is file name
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

        _task = LookupP2PContactsAsync(_cts.Token);

        // If the task is completed then return it, otherwise it's running
        return _task.IsCompleted ? _task : Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken _)
    {
        return Task.CompletedTask;
    }

    private async Task LookupP2PContactsAsync(CancellationToken ct)
    {
        const int PageSize = 100;
        var pageNum = 1;
        var contacts = new List<PotaLogEntry>();
        int cnt;

        do
        {
            var url = $"/{settings.Value.LogbookRoute}?hunterOnly=1&page={pageNum}&size={PageSize}&p2pOnly=1&startDate={_args[1]}";
            if (_args.Length > 2) url += $"&endDate={_args[2]}";
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
            pageNum++;
        } while (pageNum * PageSize < cnt);

        if (contacts.Count > 0)
        {
            logger.LogProcessing(contacts.Count);
            await UpdateLog(contacts, ct);
        }
        else
            logger.LogNoQsos();

        appLifeTime.StopApplication();
    }

    [SuppressMessage("ReSharper.DPA", "DPA0007: Large number of DB records")]
    private async Task UpdateLog(List<PotaLogEntry> potaLog, CancellationToken ct)
    {
        var notFoundCalls = new List<PotaLogEntry>();
        var startDate = DateTime.Parse(_args[1]).AddMinutes(-5);

        var endDate = DateTime.MaxValue;
        if (_args.Length == 3)
            endDate = DateTime.Parse(_args[2]).AddDays(1);

        var hrdLog = await _dbContext!.PotaContacts
            .AsTracking()
            .Include(x => x.Log)
            .Where(x => x.Log.ColTimeOn >= startDate && x.Log.ColTimeOn < endDate)
            .ToListAsync(ct);

        foreach (var potaQso in potaLog)
        {
            var found = false;

            foreach (var q in hrdLog.Where(hrdQso =>
                         hrdQso.Log.ColCall == potaQso.StationCallsign &&
                         hrdQso.Log.ColBand!.Equals(potaQso.Band, StringComparison.OrdinalIgnoreCase) &&
                         hrdQso.Log.ColTimeOn > potaQso.QsoDateTime.AddMinutes(-5) &&
                         hrdQso.Log.ColTimeOn < potaQso.QsoDateTime.AddMinutes(5)))
            {
                found = true;
                logger.LogQso(potaQso.StationCallsign.PadRight(15), potaQso.Reference.PadRight(12), $"{potaQso.Name}{(q.P2P == null ? "" : " (exists)")}");

                if (q.P2P != null)
                    continue;

                q.P2P = q.P2P == null ? potaQso.Reference : $"{q.P2P},{potaQso.Reference}";

                var park = _dbContext!.PotaParks.FirstOrDefault(x => x.ParkNum == potaQso.Reference) ?? await LoadPark(potaQso.Reference, ct);
                if (park == null)
                {
                    logger.LogParkNotFound(potaQso.Reference);
                    continue;
                }

                q.Lat = park.Lat;
                q.Long = park.Long;
                q.QrzGeoLoc = "pota";
            }

            if (!found)
                notFoundCalls.Add(potaQso);
        }

        if (notFoundCalls.Count > 0)
        {
            var calls = string.Join(',', notFoundCalls);
            var color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"The following {notFoundCalls.Count} calls were not found, probably time mismatch:");
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            foreach (var q in notFoundCalls)
            {
                Console.WriteLine($"    {q.QsoDateTime:yy-MM-dd HH:mm}   {q.StationCallsign}\t{q.Band}\t{q.Mode}");
            }
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Save changes (Y/n)? ");
            Console.ForegroundColor = color;

            if (Console.ReadKey().KeyChar != 'Y')
                return;

            logger.LogCallsNotFound(calls);
        }

        if (!_dbContext.ChangeTracker.HasChanges())
            logger.LogNoNewQsos();
        else
            await _dbContext.SaveChangesAsync(CancellationToken.None);
    }

    private async Task<PotaPark?> LoadPark(string parkNum, CancellationToken ct)
    {
        var park = _newParks.FirstOrDefault(x => x.ParkNum == parkNum);
        if (park != null)
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
        var result = JsonSerializer.Deserialize<PotaParkInfo>(res, _jsonOptions);
        if (result == null)
        {
            logger.LogJsonError(url);
            return null;
        }

        park = new PotaPark
        {
            ParkNum = parkNum,
            ParkName = result.Name,
            Lat = result.Latitude,
            Long = result.Longitude,
            Grid = result.Grid6,
            Location = result.LocationDesc,
            Country = result.ReferencePrefix
        };

        _newParks.Add(park);
        logger.LogNewPark(park.ParkNum, park.ParkName);
        _dbContext!.PotaParks.Add(park);
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
