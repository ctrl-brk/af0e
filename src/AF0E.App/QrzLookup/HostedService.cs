using AF0E.Services.Qrz;
using AF0E.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace QrzLookup;

internal sealed class HostedService(ILogger<HostedService> logger, IHostApplicationLifetime appLifeTime, IOptions<AppSettings> settings, IQrzService qrzService) : IHostedService, IDisposable
{
    private Task? _task;
    private CancellationTokenSource? _cts;
    private HrdDbContext? _dbContext;

    public Task StartAsync(CancellationToken ct)
    {
        logger.LogStarted();

        _dbContext = new HrdDbContext(settings.Value.ConnectionString);
        // Create a linked token, so we can trigger cancellation outside of this token's cancellation
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

        _task = LookupPotaContactsAsync(_cts.Token);

        // If the task is completed then return it, otherwise it's running
        return _task.IsCompleted ? _task : Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken _)
    {
        return Task.CompletedTask;
    }

    private async Task LookupPotaContactsAsync(CancellationToken ct)
    {
        var contacts = await _dbContext!.PotaContacts
            .AsTracking()
            .Where(x => x.Lat == null && x.P2P == null && (x.QrzLookupDate == null || EF.Functions.DateDiffHour(x.QrzLookupDate, DateTime.UtcNow) > 720)) //1 month
            .Include(x => x.Log)
            .Include(x => x.Activation)
            .ThenInclude(x => x.Park)
            .ToListAsync(ct);

        var cnt = 0;
        foreach (var qso in contacts)
        {
            var (response, notFound) = await qrzService.QueryCallsignAsync(qso.Log.ColCall, ct);

            if (notFound)
            {
                // QRZ has issues with slash.
                var callParts = qso.Log.ColCall.Split('/');
                if (callParts.Length == 2)
                {
                    var call = callParts[0].Length > callParts[1].Length ? callParts[0] : callParts[1];
                    (response, notFound) = await qrzService.QueryCallsignAsync(call, ct);
                    if (notFound)
                    {
                        logger.LogCallNotFound(call);
                        qso.QrzLookupDate = DateTime.MaxValue;
                        cnt++;
                        continue;
                    }
                }
                else
                {
                    logger.LogCallNotFound(qso.Log.ColCall);
                    qso.QrzLookupDate = DateTime.MaxValue;
                    cnt++;
                    continue;
                }
            }

            if (response == null) // error, but we want to save any changes made so far
                break;

            cnt++;
            logger.LogCurrentCall(qso.Log.ColPrimaryKey, qso.Log.ColCall, response.Callsign.lat, response.Callsign.lon, response.Callsign.geoloc, cnt, contacts.Count);

            if (!string.IsNullOrEmpty(response.Callsign.geoloc) && response.Callsign.geoloc != "none")
            {
                qso.Lat = response.Callsign.lat;
                qso.Long = response.Callsign.lon;
                qso.QrzGeoLoc = response.Callsign.geoloc;
            }

            qso.QrzLookupDate = DateTime.UtcNow;

            if (settings.Value.BatchSize > 0 && cnt % settings.Value.BatchSize == 0)
                if (!(await SaveChanges())) break;
        }

        await SaveChanges();

        appLifeTime.StopApplication();
    }

    private async Task<bool> SaveChanges()
    {
        try
        {
            var cnt = await _dbContext!.SaveChangesAsync();
            logger.LogUpdated(cnt);
            return true;
        }
        catch (Exception e)
        {
            logger.LogException(e);
        }
        return false;
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _dbContext?.Dispose();
    }
}
