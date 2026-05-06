using System.Net;
using AF0E.Common.Utils;
using AF0E.DB;
using AF0E.DB.Models;
using Logbook.Api.Extensions;
using Logbook.Api.Models;
using Logbook.Api.Realtime;
using Logbook.Api.Responses;
using Logbook.Api.Security;
using Logbook.Api.Validators;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using AF0E.Services.Qrz;

namespace Logbook.Api.Handlers;

public static class LogbookHandlers
{
    public static async Task<List<QsoSummary>> GetLogByCall(string call, HrdDbContext dbContext)
    {
        return [.. (await dbContext.Log.Where(x => x.ColCall == call).OrderByDescending(x => x.ColTimeOn).ToListAsync()).Select(x => new QsoSummary(x))];
    }

    public static async Task<List<string>> GetPartialLookup(string call, int maxResults, HrdDbContext dbContext)
    {
        if (string.IsNullOrEmpty(call))
            return [];

        // select top({maxResults * 5}) is a nasty hack, but I can't figure out how to make this work any other way
        return await dbContext.Database.SqlQuery<string>($"""
                                                          select distinct top({maxResults}) COL_CALL
                                                            from (select top({maxResults * 5}) col_call
                                                                    from TABLE_HRD_CONTACTS_V01
                                                                   where COL_CALL like {WebUtility.UrlDecode(call) + "%"}
                                                                   order by COL_TIME_ON desc) as a
                                                          """).ToListAsync();
    }

    public static async Task<LogSearchResponse> GetLog(string? call, int? skip, int? take, string? sort, int? orderBy, string? begin, string? end, HrdDbContext dbContext, IAuthorizationService authSvc, IHttpContextAccessor httpContext)
    {
        const int DEFAULT_PAGE_SIZE = 50;
        const int MAX_PAGE_SIZE = 500;

        skip ??= 0;
        if (take is null or > MAX_PAGE_SIZE) take = DEFAULT_PAGE_SIZE;

        call = WebUtility.UrlDecode(call);

#pragma warning disable CA1305
        var countQuery = begin is null || end is null ?
            dbContext.Log.CountAsync(x => call == null || x.ColCall == call) :
            dbContext.Log.CountAsync(x => (call == null || x.ColCall == call) && x.ColTimeOn >= DateTime.Parse(begin) && x.ColTimeOn <= DateTime.Parse(end).AddDays(1));

        var cnt = await countQuery;

        var logQuery = begin is null || end is null ?
            dbContext.Log.Where(x => call == null || x.ColCall == call) :
            dbContext.Log.Where(x => (call == null || x.ColCall == call) && x.ColTimeOn >= DateTime.Parse(begin) && x.ColTimeOn <= DateTime.Parse(end).AddDays(1));
#pragma warning restore CA1305

        logQuery = logQuery.Include(c => c.PotaContacts);

        logQuery = orderBy == 1 ? logQuery.OrderBy(x => x.ColTimeOn) : logQuery.OrderByDescending(x => x.ColTimeOn);
        logQuery = logQuery.Skip(skip.Value).Take(take.Value);

        var isAdmin = await AuthHelper.HasPolicyAsync(Policies.AdminOnly, authSvc, httpContext);
        var qsoList = await logQuery.Select(x => new QsoSummary(x, isAdmin)).ToListAsync();

        return new LogSearchResponse { TotalCount = cnt, Contacts = qsoList };
    }

    public static async Task<QsoDetails?> GetQsoDetails(int logId, HrdDbContext dbContext, IAuthorizationService authSvc, IHttpContextAccessor httpContext)
    {
        var log = await dbContext.Log
            .Include(h => h.PotaHunting)
            .Include(x => x.PotaContacts)
            .ThenInclude(c => c.Activation)
            .ThenInclude(p => p.Park)
            .SingleOrDefaultAsync(x => x.ColPrimaryKey == logId);

        if (log == null)
            return null;

        var isAdmin = await AuthHelper.HasPolicyAsync(Policies.AdminOnly, authSvc, httpContext);

        var qso = new QsoDetails(log, isAdmin);

        return qso;
    }

    public static async Task<QsoDetails> CreateQso(QsoDetails qso, int? activationId, IQrzService qrzSvc,
        IAuthorizationService authSvc, HrdDbContext dbContext, IHttpContextAccessor httpContext, ILogEventsPublisher eventsPublisher, CancellationToken ct)
    {
        QrzResponse? qrz = null;

        QsoDetailsValidator.ValidateAndThrow(qso);

        PotaActivation? activation = null;
        if (activationId is not null)
        {
            activation = await dbContext.PotaActivations
                             .Include(p => p.Park)
                             .SingleOrDefaultAsync(x => x.ActivationId == activationId.Value, ct)
                         ?? throw new ArgumentException("Invalid activation ID", nameof(activationId));

            qso.StationCallsign ??= activation.StationCallsign;
            qso.OperatorCallsign ??= activation.OperatorCallsign;
        }

        //not necessary now, but if other users added...
        //var isAdmin = await AuthHelper.HasPolicyAsync(Policies.AdminOnly, authSvc, httpContext);

        qso.Band = NormalizeBand(qso.Band)!;
        var log = qso.ToHrdLog(includeAdminFields: true /*isAdmin*/);
        log.ColQsoComplete = "Y";

        if (string.IsNullOrEmpty(log.ColName) || activationId is not null)
        {
            qrz = await QrzHandlers.Lookup(log.ColCall, qrzSvc, ct);
            log.UpdateFromQrzLookup(qrz);
        }

        if (activation is not null)
            AddActivationComment(log, activation.Park);

        dbContext.Log.Add(log);
        await dbContext.SaveChangesAsync(ct);

        if (activationId is not null)
            await PotaHandlers.AddActivationQso(activationId.Value, log, qrz, dbContext, ct);

        await eventsPublisher.PublishAsync(new LogChangedEvent(
            Operation: "created",
            LogId: log.ColPrimaryKey,
            ActivationId: activationId,
            Call: log.ColCall,
            Source: ResolveSource(httpContext.HttpContext),
            OccurredUtc: DateTime.UtcNow,
            Version: CreateEventVersion()), ct);

        return (await GetQsoDetails(log.ColPrimaryKey, dbContext, authSvc, httpContext))!;
    }

    public static async Task<QsoDetails?> UpdateQsoDetails(QsoDetails qso, HrdDbContext dbContext, IAuthorizationService authSvc, IHttpContextAccessor httpContext, ILogEventsPublisher eventsPublisher)
    {
        QsoDetailsValidator.ValidateAndThrow(qso);

        var log = await dbContext.Log
            .AsTracking()
            .SingleOrDefaultAsync(x => x.ColPrimaryKey == qso.Id);

        if (log == null)
            return null;

        var isAdmin = await AuthHelper.HasPolicyAsync(Policies.AdminOnly, authSvc, httpContext);

        qso.Band = NormalizeBand(qso.Band)!;
        log.UpdateFromQsoDetails(qso, includeAdminFields: isAdmin);

        await dbContext.SaveChangesAsync();

        await eventsPublisher.PublishAsync(new LogChangedEvent(
            Operation: "updated",
            LogId: log.ColPrimaryKey,
            ActivationId: null,
            Call: log.ColCall,
            Source: ResolveSource(httpContext.HttpContext),
            OccurredUtc: DateTime.UtcNow,
            Version: CreateEventVersion()));

        return await GetQsoDetails(log.ColPrimaryKey, dbContext, authSvc, httpContext);
    }

    public static async Task DeleteQso(int id, HrdDbContext dbContext, CancellationToken ct)
    {
        var log = dbContext.Log.AsTracking().SingleOrDefault(x => x.ColPrimaryKey == id);

        if (log == null)
            return;

        // Remove dependent contacts first (FK to HrdLog uses ClientSetNull).
        await dbContext.PotaContacts
            .Where(x => x.LogId == log.ColPrimaryKey)
            .ExecuteDeleteAsync(ct);

        dbContext.Log.Remove(log);
        await dbContext.SaveChangesAsync(ct);
    }

    public static async Task<AdifImportResponse> UploadAdif(IFormFile file, int? activationId, IQrzService qrzSvc, HrdDbContext dbContext, ILogEventsPublisher eventsPublisher, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(file);

        PotaActivation? activation = null;

        if (activationId is not null)
        {
            activation = await dbContext.PotaActivations
                .Include(p => p.Park)
                .SingleOrDefaultAsync(x => x.ActivationId == activationId.Value, ct)
                         ?? throw new ArgumentException("Invalid activation ID", nameof(activationId));
        }

        string content;
        await using (var stream = file.OpenReadStream())
        using (var reader = new StreamReader(stream))
        {
            content = await reader.ReadToEndAsync(ct);
        }

        var records = AdifParser.Parse(content);
        if (records.Count == 0)
            return new AdifImportResponse(0, 0, [], 0);

        var importedLogs = new List<HrdLog>(records.Count);
        var uploadedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var skipped = new List<string>();

        foreach (var record in records)
        {
            ct.ThrowIfCancellationRequested();

            if (!TryCreateLogEntry(record, activation, out var log))
            {
                var skippedCall = record["CALL"]?.Trim().ToUpperInvariant();
                if (!string.IsNullOrWhiteSpace(skippedCall))
                    skipped.Add(skippedCall);
                continue;
            }

            var key = CreateLogDedupKey(log);
            if (!uploadedKeys.Add(key))
            {
                skipped.Add(log.ColCall);
                continue;
            }

            importedLogs.Add(log);
        }

        if (importedLogs.Count == 0)
            return new AdifImportResponse(records.Count, 0, SortSkipped(skipped), 0);

        var calls = importedLogs.Select(x => x.ColCall).Distinct().ToList();
        var minTime = importedLogs.Min(x => x.ColTimeOn)!.Value;
        var maxTime = importedLogs.Max(x => x.ColTimeOn)!.Value;

        var existingKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var existingLogs = await dbContext.Log
            .Where(x => calls.Contains(x.ColCall) && x.ColTimeOn != null && x.ColTimeOn >= minTime && x.ColTimeOn <= maxTime)
            .Select(x => new { x.ColCall, x.ColTimeOn, x.ColBand, x.ColMode })
            .ToListAsync(ct);

        foreach (var existing in existingLogs)
        {
            if (existing.ColTimeOn is null)
                continue;

            existingKeys.Add(CreateLogDedupKey(existing.ColCall, existing.ColTimeOn.Value, existing.ColBand, existing.ColMode));
        }

        var newLogs = importedLogs
            .Where(log => !existingKeys.Contains(CreateLogDedupKey(log)))
            .ToList();

        var duplicateLogs = importedLogs
            .Where(log => existingKeys.Contains(CreateLogDedupKey(log)))
            .ToList();
        skipped.AddRange(duplicateLogs.Select(log => log.ColCall));

        if (activation is not null)
        {
            //adif could have a log for multiple days, but we need activation day only
            var activationDate = activation.StartDate.Date;
            var logsOnActivationDate = newLogs
                .Where(log => log.ColTimeOn.HasValue && log.ColTimeOn.Value.Date == activationDate)
                .ToList();
            var outOfActivationDateLogs = newLogs
                .Where(log => !log.ColTimeOn.HasValue || log.ColTimeOn.Value.Date != activationDate)
                .ToList();
            skipped.AddRange(outOfActivationDateLogs.Select(log => log.ColCall));

            //keep only the latest qso (remove dups) for each call/band/mode combination
            var deduplicatedLogs = logsOnActivationDate
                .GroupBy(log => (log.ColCall, log.ColBand, log.ColMode))
                .Select(grp => grp.OrderByDescending(log => log.ColTimeOn).First())
                .ToList();

            var deduplicatedKeys = new HashSet<string>(deduplicatedLogs.Select(CreateLogDedupKey), StringComparer.OrdinalIgnoreCase);
            var duplicateByCallBandMode = logsOnActivationDate
                .Where(log => !deduplicatedKeys.Contains(CreateLogDedupKey(log)))
                .ToList();
            skipped.AddRange(duplicateByCallBandMode.Select(log => log.ColCall));
            newLogs = deduplicatedLogs;
        }

        if (newLogs.Count == 0)
            return new AdifImportResponse(records.Count, 0, SortSkipped(skipped), 0);

        var qrzLookup = await QrzLookup(newLogs, qrzSvc, ct);

        await using var tx = await dbContext.Database.BeginTransactionAsync(ct);

        dbContext.Log.AddRange(newLogs);
        await dbContext.SaveChangesAsync(ct);

        if (activationId is not null)
        {
            var contacts = newLogs.Select(log =>
            {
                var contact = new PotaContact
                {
                    ActivationId = activationId.Value,
                    LogId = log.ColPrimaryKey
                };

                if (!qrzLookup.TryGetValue(log.ColCall, out var qrzInfo))
                    return contact;

                contact.Lat = qrzInfo.lat;
                contact.Long = qrzInfo.lon;
                contact.QrzGeoLoc = qrzInfo.geoloc;

                return contact;
            });

            dbContext.PotaContacts.AddRange(contacts);
            await dbContext.SaveChangesAsync(ct);
        }

        await tx.CommitAsync(ct);

        await eventsPublisher.PublishAsync(new LogChangedEvent(
            Operation: "imported",
            LogId: null,
            ActivationId: activationId,
            Call: null,
            Source: "adif",
            OccurredUtc: DateTime.UtcNow,
            Version: CreateEventVersion()), ct);

        return new AdifImportResponse(records.Count, newLogs.Count, SortSkipped(skipped), qrzLookup.Count);
    }

    private static string ResolveSource(HttpContext? httpContext)
    {
        if (string.Equals(httpContext?.User.Identity?.AuthenticationType, ApiKeyAuthenticationDefaults.Scheme, StringComparison.OrdinalIgnoreCase))
            return "rigcommander";

        return httpContext?.User.Identity?.IsAuthenticated == true ? "ui" : "system";
    }

    private static long CreateEventVersion() => DateTime.UtcNow.Ticks;

    private static List<string> SortSkipped(List<string> skipped)
        => [.. skipped.OrderBy(call => call, StringComparer.OrdinalIgnoreCase)];

    private static bool TryCreateLogEntry(AdifRecord record, PotaActivation? activation, out HrdLog log)
    {
        log = null!;

        var call = record["CALL"]?.Trim().ToUpperInvariant();
        var band = NormalizeBand(record["BAND"]);
        var mode = NormalizeUpper(record["MODE"]);

        if (string.IsNullOrWhiteSpace(call) || string.IsNullOrWhiteSpace(band) || string.IsNullOrWhiteSpace(mode))
            return false;

        if (!TryParseAdifDateTime(record["QSO_DATE"], record["TIME_ON"], out var timeOn))
            return false;

        DateTime? timeOff = null;
        if (TryParseAdifDateTime(record["QSO_DATE_OFF"] ?? record["QSO_DATE"], record["TIME_OFF"], out var parsedTimeOff))
            timeOff = parsedTimeOff;

        log = new HrdLog
        {
            ColCall = call,
            ColTimeOn = timeOn,
            ColTimeOff = timeOff ?? timeOn,
            ColBand = band,
            ColBandRx = NormalizeBand(record["BAND_RX"]),
            ColFreq = ParseDouble(record["FREQ"], 0),
            ColFreqRx = ParseDouble(record["FREQ_RX"]),
            ColMode = mode,
            ColSubmode = NormalizeUpper(record["SUBMODE"]),
            ColRstSent = NormalizeText(record["RST_SENT"]),
            ColRstRcvd = NormalizeText(record["RST_RCVD"]),
            ColName = NormalizeText(record["NAME"]),
            ColCnty = NormalizeText(record["CNTY"]),
            ColState = NormalizeUpper(record["STATE"]),
            ColCountry = NormalizeText(record["COUNTRY"]),
            ColGridsquare = NormalizeUpper(record["GRIDSQUARE"]),
            ColCqz = ParseDouble(record["CQZ"], 0),
            ColItuz = ParseDouble(record["ITUZ"], 0),
            ColDxcc = NormalizeIntString(record["DXCC"], "0"),
            ColComment = NormalizeText(record["COMMENT"]),
            ColNotes = NormalizeText(record["NOTES"]),
            ColMyCity = NormalizeText(record["MY_CITY"]),
            ColMyCnty = NormalizeText(record["MY_CNTY"]),
            ColMyState = NormalizeUpper(record["MY_STATE"]),
            ColMyCountry = NormalizeText(record["MY_COUNTRY"], "United States"),
            ColMyCqZone = ParseDouble(record["MY_CQ_ZONE"], 4),
            ColMyItuZone = ParseDouble(record["MY_ITU_ZONE"], 7),
            ColMyGridsquare = NormalizeUpper(record["MY_GRIDSQUARE"]),
            ColQslSent = NormalizeQslStatus(record["QSL_SENT"]),
            ColQslsdate = ParseAdifDate(record["QSLSDATE"]),
            ColQslSentVia = NormalizeQslVia(record["QSL_SENT_VIA"]) ?? "D",
            ColQslRcvd = NormalizeQslStatus(record["QSL_RCVD"]),
            ColQslrdate = ParseAdifDate(record["QSLRDATE"]),
            ColQslRcvdVia = NormalizeQslVia(record["QSL_RCVD_VIA"]),
            ColQslVia = NormalizeText(record["QSL_VIA"]),
            ColEqslQslSent = NormalizeQslStatus(record["EQSL_QSL_SENT"]),
            ColEqslQslsdate = ParseAdifDate(record["EQSL_QSLSDATE"]),
            ColEqslQslRcvd = NormalizeQslStatus(record["EQSL_QSL_RCVD"]),
            ColEqslQslrdate = ParseAdifDate(record["EQSL_QSLRDATE"]),
            ColLotwQslSent = NormalizeQslStatus(record["LOTW_QSL_SENT"]),
            ColLotwQslsdate = ParseAdifDate(record["LOTW_QSLSDATE"]),
            ColLotwQslRcvd = NormalizeQslStatus(record["LOTW_QSL_RCVD"]),
            ColLotwQslrdate = ParseAdifDate(record["LOTW_QSLRDATE"]),
            ColOperator = NormalizeUpper(record["OPERATOR"], "AF0E"),
            ColStationCallsign = NormalizeUpper(record["STATION_CALLSIGN"], "AF0E"),
            ColOwnerCallsign = NormalizeUpper(record["OWNER_CALLSIGN"]),
            ColContestId = NormalizeText(record["CONTEST_ID"]),
            ColPropMode = NormalizeUpper(record["PROP_MODE"]),
            ColSatName = NormalizeUpper(record["SAT_NAME"]),
            ColSatMode = NormalizeUpper(record["SAT_MODE"]),
            ColSig = NormalizeUpper(record["SIG"]),
            ColSigInfo = NormalizeText(record["SIG_INFO"]),
            ColMySig = NormalizeUpper(record["MY_SIG"]),
            ColMySigInfo = NormalizeText(record["MY_SIG_INFO"]),
            ColQth = NormalizeText(record["QTH"]),
            ColRig = NormalizeText(record["RIG"]),
            ColMyRig = NormalizeText(record["MY_RIG"]),
            ColTxPwr = ParseDouble(record["TX_PWR"], 0),
            SiteComment = NormalizeText(record["APP_AF0E_SITE_COMMENT"])
        };

        if (log.ColFreq is < 1000)
            log.ColFreq *= 1000000.0;

        if (log.ColFreqRx is < 1000)
            log.ColFreqRx *= 1000000.0;

        if (activation is null)
            return true;

        log.ColMyGridsquare = activation.Grid;
        log.ColMyCnty = activation.County;
        log.ColMyState = activation.State;
        log.ColMyCity = activation.City;
        log.ColMyLat = (double)activation.Lat;
        log.ColMyLon = (double)activation.Long;

        AddActivationComment(log, activation.Park);

        return true;
    }

    private static async Task<Dictionary<string, (decimal lat, decimal lon, string? geoloc)>> QrzLookup(List<HrdLog> newLogs, IQrzService qrzSvc, CancellationToken ct)
    {
        var ret = new Dictionary<string, (decimal lat, decimal lon, string? geoloc)>(newLogs.Count);

        foreach (var log in newLogs.Where(l => l.ColLat == 0 || l.ColLon == null))
        {
            var res = (await qrzSvc.QueryCallsignAsync(log.ColCall, ct)).response;
            if (res is null)
                continue;

            log.ColLat = (double)res.Callsign.lat;
            log.ColLon = (double)res.Callsign.lon;
            log.ColName = res.Callsign.name;
            log.ColCnty = res.Callsign.county;
            log.ColState = res.Callsign.state;
            log.ColCountry = res.Callsign.country;

            ret.TryAdd(log.ColCall, (res.Callsign.lat, res.Callsign.lon, res.Callsign.geoloc));
        }

        return ret;
    }

    private static void AddActivationComment(HrdLog log, PotaPark park)
    {
        var cmt = log.ColComment;
        if (cmt is not null && cmt.StartsWith("POTA activation", StringComparison.OrdinalIgnoreCase))
            return;

        log.ColComment = $"POTA activation {park.ParkNum} ({Utils.AbbreviateParkName(park.ParkName)})";
        if (cmt != null)
            log.ColComment += $". {cmt}";
    }

    private static string CreateLogDedupKey(HrdLog log) => CreateLogDedupKey(log.ColCall, log.ColTimeOn!.Value, log.ColBand, log.ColMode);

    private static string CreateLogDedupKey(string? call, DateTime timeOn, string? band, string? mode)
        => $"{call?.Trim().ToUpperInvariant()}|{timeOn:O}|{band?.Trim().ToUpperInvariant()}|{mode?.Trim().ToUpperInvariant()}";

    private static bool TryParseAdifDateTime(string? dateText, string? timeText, out DateTime value)
    {
        value = default;

        if (!TryParseAdifDate(dateText, out var date))
            return false;

        if (!TryParseAdifTime(timeText, out var time))
            return false;

        value = date.Add(time);
        return true;
    }

    private static DateTime? ParseAdifDate(string? dateText)
        => TryParseAdifDate(dateText, out var value) ? value : null;

    private static bool TryParseAdifDate(string? dateText, out DateTime value)
        => DateTime.TryParseExact(dateText?.Trim(), "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out value);

    private static bool TryParseAdifTime(string? timeText, out TimeSpan value)
    {
        value = TimeSpan.Zero;
        var raw = timeText?.Trim();
        if (string.IsNullOrWhiteSpace(raw) || raw.Length < 4)
            return false;

        raw = raw.Length >= 6 ? raw[..6] : raw[..4] + "00";

        if (!int.TryParse(raw.AsSpan(0, 2), out var hours) || !int.TryParse(raw.AsSpan(2, 2), out var minutes) || !int.TryParse(raw.AsSpan(4, 2), out var seconds))
            return false;

        if (hours is < 0 or > 23 || minutes is < 0 or > 59 || seconds is < 0 or > 59)
            return false;

        value = new TimeSpan(hours, minutes, seconds);
        return true;
    }

    private static double? ParseDouble(string? value, double? defaultValue = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        var normalized = value.Trim().Replace(',', '.');
        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed) ? parsed : defaultValue;
    }

    private static string? NormalizeText(string? value, string? defaultValue = null)
        => string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();

    private static string? NormalizeUpper(string? value, string? defaultValue = null)
        => NormalizeText(value, defaultValue)?.ToUpperInvariant();

    private static string? NormalizeBand(string? value)
    {
        var normalized = NormalizeUpper(value);
        return normalized switch
        {
            "160M" => "160m",
            "80M" => "80m",
            "60M" => "60m",
            "40M" => "40m",
            "30M" => "30m",
            "20M" => "20m",
            "17M" => "17m",
            "15M" => "15m",
            "12M" => "12m",
            "10M" => "10m",
            "6M" => "6m",
            "2M" => "2m",
            "1.25M" => "1.25m",
            "70CM" => "70cm",
            "33CM" => "33cm",
            "23CM" => "23cm",
            _ => NormalizeText(value)
        };
    }

    private static string? NormalizeIntString(string? value, string? defaultValue = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return defaultValue;

        return int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed) ? parsed.ToString(CultureInfo.InvariantCulture) : null;
    }

    private static string NormalizeQslStatus(string? value)
    {
        var normalized = NormalizeUpper(value);
        return normalized is "N" or "V" or "Q" or "R" or "Y" or "I" ? normalized : "N";
    }

    private static string? NormalizeQslVia(string? value)
    {
        var normalized = NormalizeUpper(value);
        return normalized is "B" or "D" or "E" or "M" ? normalized : null;
    }
}
