using System.Globalization;

namespace RigCommander.Services;

public static class AdifQsoRequestMapper
{
    public static bool TryMap(IReadOnlyDictionary<string, string> fields, int? activationId, out QsoRequestPayload payload, out string error)
    {
        payload = new QsoRequestPayload();

        if (!TryGetRequired(fields, "CALL", out var call))
        {
            error = "Missing CALL";
            return false;
        }

        var band = FirstNonEmpty(fields, "BAND");
        if (string.IsNullOrWhiteSpace(band))
        {
            error = "Missing BAND";
            return false;
        }

        var mode = FirstNonEmpty(fields, "SUBMODE", "MODE");
        if (string.IsNullOrWhiteSpace(mode))
        {
            error = "Missing MODE/SUBMODE";
            return false;
        }

        if (!TryParseQsoDateTime(fields, out var date))
        {
            error = "Missing or invalid QSO_DATE/TIME_ON";
            return false;
        }

        payload.PotaActivationId = activationId;
        payload.Qso.Call = call.ToUpperInvariant();
        payload.Qso.Band = band.Trim().ToUpperInvariant();
        payload.Qso.Mode = mode.Trim().ToUpperInvariant();
        payload.Qso.Date = date;

        payload.Qso.RstSent = FirstNonEmpty(fields, "RST_SENT");
        payload.Qso.RstRcvd = FirstNonEmpty(fields, "RST_RCVD");
        payload.Qso.Name = FirstNonEmpty(fields, "NAME");
        payload.Qso.State = FirstNonEmpty(fields, "STATE");
        payload.Qso.Country = FirstNonEmpty(fields, "COUNTRY");
        payload.Qso.Grid = FirstNonEmpty(fields, "GRIDSQUARE");
        payload.Qso.MyGrid = FirstNonEmpty(fields, "MY_GRIDSQUARE");
        payload.Qso.Comment = FirstNonEmpty(fields, "COMMENT");
        if (TryParseDouble(fields, "FREQ", out var freqMhz))
            payload.Qso.Freq = freqMhz < 1000 ? freqMhz * 1000000 : freqMhz; //we need to convert MHz to Hz for the API

        if (TryParseDouble(fields, "FREQ_RX", out var freqRxMhz))
            payload.Qso.FreqRx = freqRxMhz < 1000 ? freqRxMhz * 1000000 : freqRxMhz;

        error = string.Empty;
        return true;
    }

    private static bool TryParseQsoDateTime(IReadOnlyDictionary<string, string> fields, out DateTime dateTimeUtc)
    {
        dateTimeUtc = default;

        if (!TryGetRequired(fields, "QSO_DATE", out var qsoDate))
            return false;

        if (!DateOnly.TryParseExact(qsoDate, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return false;

        var timeRaw = FirstNonEmpty(fields, "TIME_ON") ?? "000000";
        timeRaw = timeRaw.Replace(":", string.Empty, StringComparison.Ordinal);

        if (timeRaw.Length == 4)
            timeRaw += "00";

        if (timeRaw.Length < 6)
            timeRaw = timeRaw.PadRight(6, '0');

        if (!TimeOnly.TryParseExact(timeRaw[..6], "HHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
            return false;

        dateTimeUtc = DateTime.SpecifyKind(date.ToDateTime(time), DateTimeKind.Utc);
        return true;
    }

    private static bool TryParseDouble(IReadOnlyDictionary<string, string> fields, string key, out double value)
    {
        value = 0;
        var text = FirstNonEmpty(fields, key);
        return !string.IsNullOrWhiteSpace(text) && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryGetRequired(IReadOnlyDictionary<string, string> fields, string key, out string value)
    {
        value = string.Empty;

        if (!fields.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
            return false;

        value = raw.Trim();
        return true;
    }

    private static string? FirstNonEmpty(IReadOnlyDictionary<string, string> fields, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!fields.TryGetValue(key, out var value) || string.IsNullOrWhiteSpace(value))
                continue;

            return value.Trim();
        }

        return null;
    }
}

// ReSharper disable UnusedAutoPropertyAccessor.Global

public sealed class QsoRequestPayload
{
    public int? PotaActivationId { get; set; }
    public QsoPayload Qso { get; set; } = new();
}

public sealed class QsoPayload
{
    public DateTime Date { get; set; }
    public string Call { get; set; } = string.Empty;
    public string Band { get; set; } = string.Empty;
    public string Mode { get; set; } = string.Empty;
    public string? RstSent { get; set; }
    public string? RstRcvd { get; set; }
    public string? Name { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? Grid { get; set; }
    public string? MyGrid { get; set; }
    public double? Freq { get; set; }
    public double? FreqRx { get; set; }
    public string? Comment { get; set; }
}
