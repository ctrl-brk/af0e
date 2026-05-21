using System.Globalization;
using AF0E.Common.Radio;
using AF0E.Common.Utils;
using Microsoft.Extensions.Options;

namespace Logbook.Api.Services;

public interface ILotwService
{
    Task<IReadOnlyList<LotwQslRecord>> GetConfirmedQslRecordsAsync(DateOnly since, CancellationToken ct = default);
}

public sealed record LotwQslRecord
{
    public required string Call { get; init; }
    public required DateOnly QsoDate { get; init; }
    public DateTime? QsoTimeUtc { get; init; }
    public string? Band { get; init; }
    public string? Mode { get; init; }
    public string? StationCallsign { get; init; }
    public DateOnly? QslReceivedDate { get; init; }
}

public sealed class LotwService(IHttpClientFactory httpClientFactory, IOptions<LotwSettings> options, ILogger<LotwService> logger) : ILotwService
{
    private static readonly Action<ILogger, int, DateOnly, Exception?> LogFetchedQslRecords =
        LoggerMessage.Define<int, DateOnly>(LogLevel.Information, new EventId(1, nameof(GetConfirmedQslRecordsAsync)), "Fetched {Count} LoTW QSL records since {Since}");

    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("lotw");
    private readonly LotwSettings _settings = options.Value;

    public async Task<IReadOnlyList<LotwQslRecord>> GetConfirmedQslRecordsAsync(DateOnly since, CancellationToken ct = default)
    {
        EnsureConfigured();

        using var request = new HttpRequestMessage(HttpMethod.Get, BuildRequestUri(since));
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        if (string.IsNullOrWhiteSpace(body))
            return [];

        if (!body.Contains("<EOH>", StringComparison.OrdinalIgnoreCase) && !body.Contains("<EOR>", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"LoTW response was not valid ADIF: {GetOneLineSnippet(body)}");

        var records = AdifParser.Parse(body);
        if (records.Count == 0)
            return [];

        var parsed = new List<LotwQslRecord>(records.Count);
        parsed.AddRange(records.Select(TryParseRecord).OfType<LotwQslRecord>());

        LogFetchedQslRecords(logger, parsed.Count, since, null);
        return parsed;
    }

    private void EnsureConfigured()
    {
        if (_settings.ApiUrl is null)
            throw new InvalidOperationException("LoTW settings are not configured.");

        if (string.IsNullOrWhiteSpace(_settings.Username) || string.IsNullOrWhiteSpace(_settings.Password))
            throw new InvalidOperationException("LoTW username or password is missing.");
    }

    private Uri BuildRequestUri(DateOnly since)
    {
        var parameters = new Dictionary<string, string?>
        {
            ["login"] = _settings.Username,
            ["password"] = _settings.Password,
            ["qso_query"] = "1",
            ["qso_qsl"] = "yes",
            ["qso_qslsince"] = since.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            ["qso_qsldetail"] = "yes",
        };

        if (!string.IsNullOrWhiteSpace(_settings.OwnCallsign))
            parameters["qso_owncall"] = _settings.OwnCallsign;

        var query = string.Join("&", parameters
            .Where(static kvp => !string.IsNullOrWhiteSpace(kvp.Value))
            .Select(static kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!)}"));

        return new UriBuilder(_settings.ApiUrl)
        {
            Query = query,
        }.Uri;
    }

    private static LotwQslRecord? TryParseRecord(AdifRecord record)
    {
        var call = record["CALL"]?.Trim().ToUpperInvariant();
        var band = RadioHelper.NormalizeBand(record["BAND"]);
        var mode = RadioHelper.NormalizeMode(record["SUBMODE"] ?? record["MODE"]);
        var stationCallsign = NormalizeCallsign(record["STATION_CALLSIGN"] ?? record["APP_LOTW_OWNCALL"] ?? record["MY_CALL"]);
        var qslReceivedDate = ParseAdifDate(record["LOTW_QSLRDATE"])
                              ?? ParseAdifDate(record["QSLRDATE"])
                              ?? ParseAdifDate(record["APP_LOTW_RXQSLDATE"])
                              ?? ParseAdifDate(record["APP_LOTW_QSLRDATE"]);

        if (string.IsNullOrWhiteSpace(call) || !TryParseAdifDate(record["QSO_DATE"], out var qsoDate))
            return null;

        var qslStatus = record["LOTW_QSL_RCVD"]
                        ?? record["QSL_RCVD"]
                        ?? record["APP_LOTW_RXQSL"]
                        ?? record["APP_LOTW_QSL_RCVD"];

        if (!IsConfirmed(qslStatus) && qslReceivedDate is null)
            return null;

        var qsoTimeUtc = TryParseAdifDateTime(record["QSO_DATE"], record["TIME_ON"], out var parsedQsoTimeUtc)
            ? (DateTime?)parsedQsoTimeUtc
            : null;

        return new LotwQslRecord
        {
            Call = call,
            QsoDate = DateOnly.FromDateTime(qsoDate),
            QsoTimeUtc = qsoTimeUtc,
            Band = band,
            Mode = mode,
            StationCallsign = stationCallsign,
            QslReceivedDate = qslReceivedDate is null ? null : DateOnly.FromDateTime(qslReceivedDate.Value),
        };
    }

    private static bool IsConfirmed(string? value)
        => value?.Trim().ToUpperInvariant() is "Y" or "V" or "R";

    private static string? NormalizeCallsign(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();

    private static bool TryParseAdifDateTime(string? dateText, string? timeText, out DateTime value)
    {
        value = default;

        if (!TryParseAdifDate(dateText, out var date))
            return false;

        if (!TryParseAdifTime(timeText, out var time))
            return false;

        value = DateTime.SpecifyKind(date.Add(time), DateTimeKind.Utc);
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

        if (!int.TryParse(raw.AsSpan(0, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var hours)
            || !int.TryParse(raw.AsSpan(2, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var minutes)
            || !int.TryParse(raw.AsSpan(4, 2), NumberStyles.Integer, CultureInfo.InvariantCulture, out var seconds))
            return false;

        if (hours is < 0 or > 23 || minutes is < 0 or > 59 || seconds is < 0 or > 59)
            return false;

        value = new TimeSpan(hours, minutes, seconds);
        return true;
    }

    private static string GetOneLineSnippet(string body)
    {
        var firstLine = body.Replace('\r', '\n').Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(firstLine))
            return "(empty response)";

        return firstLine.Length <= 200 ? firstLine : firstLine[..200];
    }
}
