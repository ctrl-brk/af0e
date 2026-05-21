namespace AF0E.Common.Radio;

public static class RadioHelper
{
    private static readonly HashSet<string> DigitalModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "DIGI",
        "FT8",
        "FT4",
        "FT2",
        "JS8",
        "MFSK",
        "MSK144",
        "RTTY",
        "PSK",
        "JT65",
        "JT9",
        "SSTV",
        "WSPR",
        "OLIVIA",
        "DOMINO",
        "THOR",
        "HELL",
        "PACKET",
        "PKT"
    };

    public static string? DetectBand(decimal? frequencyKhz)
        => frequencyKhz switch
        {
            >= 1800m and <= 2000m => "160m",
            >= 3500m and <= 4000m => "80m",
            >= 5330m and <= 5406m => "60m",
            >= 7000m and <= 7300m => "40m",
            >= 10100m and <= 10150m => "30m",
            >= 14000m and <= 14350m => "20m",
            >= 18068m and <= 18168m => "17m",
            >= 21000m and <= 21450m => "15m",
            >= 24890m and <= 24990m => "12m",
            >= 28000m and <= 29700m => "10m",
            >= 50000m and <= 54000m => "6m",
            >= 144000m and <= 148000m => "2m",
            >= 222000m and <= 225000m => "1.25m",
            >= 420000m and <= 450000m => "70cm",
            >= 902000m and <= 928000m => "33cm",
            >= 1240000m and <= 1300000m => "23cm",
            _ => null
        };

    public static string? NormalizeBand(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim().ToUpperInvariant();
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
            _ => value.Trim()
        };
    }

    public static string? NormalizeMode(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
            return null;

        var normalized = mode.Trim().ToUpperInvariant();
        return normalized switch
        {
            "USB" or "LSB" or "SSB" or "PHONE" => "SSB",
            "DIGI" or "DIGITAL" or "DATA" => "DIGI",
            "FT-8" => "FT8",
            "FT-4" => "FT4",
            "FT-2" => "FT2",
            "JS8CALL" => "JS8",
            "FSK" => "RTTY",
            _ when normalized.StartsWith("MFSK", StringComparison.Ordinal) => "MFSK",
            _ when normalized.StartsWith("PSK", StringComparison.Ordinal) => "PSK",
            _ => normalized
        };
    }

    public static bool IsDigitalMode(string? mode)
    {
        var normalized = NormalizeMode(mode);
        return normalized is not null && DigitalModes.Contains(normalized);
    }

    public static bool ModesMatch(string? left, string? right)
    {
        var normalizedLeft = NormalizeMode(left);
        var normalizedRight = NormalizeMode(right);
        if (normalizedLeft is null || normalizedRight is null)
            return false;

        if (string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase))
            return true;

        return IsGenericDigitalMode(normalizedLeft) && IsDigitalMode(normalizedRight)
            || IsGenericDigitalMode(normalizedRight) && IsDigitalMode(normalizedLeft);
    }

    private static bool IsGenericDigitalMode(string mode)
        => string.Equals(mode, "DIGI", StringComparison.OrdinalIgnoreCase);
}


