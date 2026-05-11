using System.Globalization;
using System.Text.RegularExpressions;
using AF0E.Services.DxCluster.Models;

namespace AF0E.Services.DxCluster;

internal static partial class DxClusterSpotParser
{
    public static bool TryParse(string sourceName, string rawLine, DateTimeOffset receivedAtUtc, out DxClusterSpot? spot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
        ArgumentNullException.ThrowIfNull(rawLine);

        var match = SpotLineRegex().Match(rawLine);
        if (!match.Success)
        {
            spot = null;
            return false;
        }

        var frequencyText = match.Groups["frequency"].Value;
        if (!decimal.TryParse(frequencyText, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var frequencyKhz))
        {
            spot = null;
            return false;
        }

        var spotter = SanitizeField(match.Groups["spotter"].Value);
        var dxCall = SanitizeField(match.Groups["dx"].Value);
        var comment = SanitizeField(match.Groups["comment"].Value);
        var sanitizedRawLine = SanitizeField(rawLine);
        var spotTimeUtc = ParseSpotTimeUtc(match.Groups["time"].Value, receivedAtUtc);

        spot = new DxClusterSpot
        {
            SourceName = sourceName,
            SpotterCallsign = spotter,
            DxCallsign = dxCall,
            FrequencyKhz = frequencyKhz,
            Mode = DxClusterSpotModeDetector.Detect(frequencyKhz, comment, sanitizedRawLine),
            Comment = comment,
            RawLine = sanitizedRawLine,
            SpotTimeUtc = spotTimeUtc,
            ReceivedAtUtc = receivedAtUtc
        };

        return true;
    }

    private static DateTimeOffset ParseSpotTimeUtc(string hhmmText, DateTimeOffset receivedAtUtc)
    {
        if (hhmmText.Length != 4 ||
            !int.TryParse(hhmmText.AsSpan(0, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var hours) ||
            !int.TryParse(hhmmText.AsSpan(2, 2), NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) ||
            hours is < 0 or > 23 ||
            minutes is < 0 or > 59)
        {
            return receivedAtUtc;
        }

        var candidate = new DateTimeOffset(receivedAtUtc.UtcDateTime.Date.AddHours(hours).AddMinutes(minutes), TimeSpan.Zero);
        if (candidate > receivedAtUtc.AddMinutes(5))
            candidate = candidate.AddDays(-1);

        return candidate;
    }

    private static string SanitizeField(string value)
        => new string([.. value.Where(static ch => !char.IsControl(ch))]).Trim();

    // Matches a typical DX cluster spot line such as:
    //   DX de AF0E:  14027.5  P5RS7        up 1                           2359Z
    // Breakdown:
    // - ^\s*DX\s+de\s+       - accepts optional leading whitespace, then the literal "DX de"
    // - (?<spotter>[^:]+):   - captures the posting station callsign/text up to the first colon
    // - (?<frequency>...)    - captures the spotted frequency in kHz, allowing an optional decimal part
    // - (?<dx>\S+)           - captures the spotted DX callsign/token
    // - (?<comment>.*?)      - captures the remainder of the comment text lazily
    // - (?<time>\d{4})Z      - optionally captures the trailing UTC spot time in HHMMZ format
    // - \s*$                 - ignores any trailing whitespace at the end of the line
    [GeneratedRegex(@"^\s*DX\s+de\s+(?<spotter>[^:]+):\s+(?<frequency>\d+(?:\.\d+)?)\s+(?<dx>\S+)\s+(?<comment>.*?)(?:\s+(?<time>\d{4})Z)?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SpotLineRegex();
}
