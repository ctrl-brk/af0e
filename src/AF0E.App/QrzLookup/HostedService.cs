using System.Web;
using System.Xml;
using System.Xml.Serialization;
using AF0E.DB;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace QrzLookup;

internal sealed class HostedService(ILogger<HostedService> logger, IHostApplicationLifetime appLifeTime, IOptions<AppSettings> settings) : IHostedService, IDisposable
{
    private const string Agent = "af0e_lookup";
    private Task? _task;
    private CancellationTokenSource? _cts;
    private HttpClient? _httpClient;
    private string? _sessionKey;
    private HrdDbContext? _dbContext;

    public Task StartAsync(CancellationToken ct)
    {
        logger.LogStarted();

        _dbContext = new HrdDbContext(settings.Value.ConnectionString);
        _httpClient = new HttpClient { BaseAddress = settings.Value.QrzApiUrl };
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
            .Where(x => x.Lat == null && x.P2P == null && (x.QrzLookupDate == null || EF.Functions.DateDiffMinute(x.QrzLookupDate, DateTime.UtcNow) > 43800)) //1 month
            .Include(x => x.Log)
            .Include(x => x.Activation)
            .ThenInclude(x => x.Park)
            .ToListAsync(ct);

        var cnt = 0;
        foreach (var qso in contacts)
        {
            var (response, notFound) = await QueryQrz(qso.Log.ColCall, ct);

            if (notFound)
            {
                // QRZ has issues with slash.
                var callParts = qso.Log.ColCall.Split('/');
                if (callParts.Length == 2)
                {
                    var call = callParts[0].Length > callParts[1].Length ? callParts[0] : callParts[1];
                    (response, notFound) = await QueryQrz(call, ct);
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

    private async Task<(QRZDatabase? response, bool notFound)> QueryQrz(string callSign, CancellationToken ct)
    {
        var retry = false;
        QRZDatabase? result;

    RETRY_SESSION:
        await GetSessionKey(ct);

        if (string.IsNullOrEmpty(_sessionKey))
            return (null, false);

        var response = await _httpClient!.GetAsync($"?s={_sessionKey};callsign={HttpUtility.UrlEncode(callSign)};agent={Agent}", ct);
        //for debug: response.Content.ReadAsStringAsync()
        await using (var stream = await response.Content.ReadAsStreamAsync(ct))
        {
            using var reader = XmlReader.Create(stream);
            var serializer = new XmlSerializer(typeof(QRZDatabase), "http://xmldata.qrz.com");
            try
            {
                result = serializer.Deserialize(reader) as QRZDatabase;
            }
            catch (Exception e)
            {
                logger.LogInvalidXml(await response.Content.ReadAsStringAsync(ct), e);
                return (null, false);
            }
        }

        if (result?.Session.Error is null) //everything is OK
            return (result, false);

        if (string.Equals(result.Session.Error, "Invalid session key", StringComparison.OrdinalIgnoreCase) && !retry)
        {
            _sessionKey = null;
            retry = true;
            goto RETRY_SESSION; // :))
        }

        if (result.Session.Error.StartsWith("Not found", StringComparison.OrdinalIgnoreCase))
            return (null, true);

        logger.LogInvalidQrzResponse(result.Session.Error);
        return (null, false);
    }

    private async Task GetSessionKey(CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_sessionKey))
            return;

        if (string.IsNullOrEmpty(settings.Value.QrzUser) || string.IsNullOrEmpty(settings.Value.QrzPassword))
        {
            logger.LogConfigurationError("Qrz XML user name or password is missing.");
            return;
        }

        var response = await _httpClient!.GetAsync($"?username={settings.Value.QrzUser};password={settings.Value.QrzPassword};agent={Agent}", ct);
        var body = await response.Content.ReadAsStringAsync(ct);
        var xmlDoc = new XmlDocument();
        xmlDoc.LoadXml(body);
        var node = xmlDoc.SelectSingleNode("/*[local-name()='QRZDatabase']/*[local-name()='Session']");
        if (node is null)
        {
            _sessionKey = null;
            logger.LogInvalidQrzResponse(body);
            return;
        }

        var key = node["Key"]?.InnerText;
        if (!string.IsNullOrEmpty(key))
        {
            _sessionKey = key;
            return;
        }

        logger.LogQrzApiError(node["Error"]?.InnerText, node["Message"]?.InnerText);
        _sessionKey = null;
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
        _cts?.Dispose();
        _httpClient?.Dispose();
        _dbContext?.Dispose();
    }
}
