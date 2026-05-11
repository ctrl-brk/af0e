using System.Text.RegularExpressions;

namespace AF0E.Services.DxCluster;

internal static partial class DxClusterSpotModeDetector
{
    private static readonly IReadOnlyList<ModeWindow> FrequencyModeWindows =
    [
        new(1800m, 1838m, "CW"),
        new(1838m, 1843m, "DIGI"),
        new(1843m, 2000m, "SSB"),

        new(3500m, 3570m, "CW"),
        new(3570m, 3600m, "DIGI"),
        new(3600m, 4000m, "SSB"),

        new(7000m, 7040m, "CW"),
        new(7040m, 7125m, "DIGI"),
        new(7125m, 7300m, "SSB"),

        new(10100m, 10130m, "CW"),
        new(10130m, 10150m, "DIGI"),

        new(14000m, 14070m, "CW"),
        new(14070m, 14112m, "DIGI"),
        new(14112m, 14350m, "SSB"),

        new(18068m, 18100m, "CW"),
        new(18100m, 18110m, "DIGI"),
        new(18110m, 18168m, "SSB"),

        new(21000m, 21070m, "CW"),
        new(21070m, 21150m, "DIGI"),
        new(21150m, 21450m, "SSB"),

        new(24890m, 24920m, "CW"),
        new(24920m, 24930m, "DIGI"),
        new(24930m, 24990m, "SSB"),

        new(28000m, 28070m, "CW"),
        new(28070m, 28190m, "DIGI"),
        new(28300m, 29000m, "SSB"),
        new(29000m, 29700m, "FM"),

        new(50000m, 50100m, "CW"),
        new(50100m, 50300m, "DIGI"),
        new(50300m, 54000m, "SSB")
    ];

    public static string? Detect(decimal frequencyKhz, string? comment, string? rawLine)
        => TryDetectFromText(comment)
           ?? TryDetectFromText(rawLine)
           ?? TryDetectFromFrequency(frequencyKhz);

    public static string? NormalizeMode(string? mode)
    {
        if (string.IsNullOrWhiteSpace(mode))
            return null;

        var normalized = mode.Trim().ToUpperInvariant();
        return normalized switch
        {
            "USB" or "LSB" or "SSB" or "PHONE" => "SSB",
            "DIGI" or "DIGITAL" or "DATA" => "DIGI",
            _ => normalized
        };
    }

    private static string? TryDetectFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var uppercase = text.ToUpperInvariant();
        var tokens = WordTokenRegex().Matches(uppercase).Select(static match => match.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (tokens.Contains("FT8") || uppercase.Contains("FT-8", StringComparison.Ordinal))
            return "FT8";

        if (tokens.Contains("FT4") || uppercase.Contains("FT-4", StringComparison.Ordinal))
            return "FT4";

        if (tokens.Contains("JS8") || uppercase.Contains("JS8CALL", StringComparison.Ordinal))
            return "JS8";

        if (tokens.Contains("MSK144"))
            return "MSK144";

        if (tokens.Contains("RTTY") || tokens.Contains("FSK"))
            return "RTTY";

        if (tokens.Contains("PSK31") || tokens.Contains("PSK63") || tokens.Contains("PSK"))
            return "PSK";

        if (tokens.Contains("JT65"))
            return "JT65";

        if (tokens.Contains("JT9"))
            return "JT9";

        if (tokens.Contains("SSTV"))
            return "SSTV";

        if (tokens.Contains("WSPR"))
            return "WSPR";

        if (tokens.Contains("CW"))
            return "CW";

        if (tokens.Contains("USB") || tokens.Contains("LSB") || tokens.Contains("SSB") || tokens.Contains("PHONE"))
            return "SSB";

        if (tokens.Contains("FM"))
            return "FM";

        if (tokens.Contains("AM"))
            return "AM";

        if (tokens.Contains("DIGI") || tokens.Contains("DIGITAL") || tokens.Contains("DATA"))
            return "DIGI";

        return null;
    }

    private static string? TryDetectFromFrequency(decimal frequencyKhz)
        => FrequencyModeWindows.FirstOrDefault(window => window.Contains(frequencyKhz)).Mode;

    [GeneratedRegex("[A-Z0-9]+", RegexOptions.CultureInvariant)]
    private static partial Regex WordTokenRegex();

    private readonly record struct ModeWindow(decimal MinKhz, decimal MaxKhz, string Mode)
    {
        public bool Contains(decimal frequencyKhz) => frequencyKhz >= MinKhz && frequencyKhz <= MaxKhz;
    }
}
