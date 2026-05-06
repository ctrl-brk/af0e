using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace PotaHuntingLookup;

#pragma warning disable CA1052
[SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
public partial class Helpers
{
    /// <summary>
    /// Regex pattern explanation:
    /// ^                                       → Anchor at the start of the string
    /// (?:                                     → Begin non-capturing group (two alternatives)
    ///   POTA\s+[A-Z]{2}-\d{4,5}               → "POTA", a space, 2 letters, a dash, and 4–5 digits
    ///   \s*[.,;|]?\s*                         → Optional whitespace, optional punctuation (. , ; |), optional whitespace
    ///   (?:\r?\n)?                            → Optional line break (CRLF or LF)
    ///   |                                     → OR
    ///   POTA\s*[.,;|]?\s*                     → "POTA" by itself (optionally with punctuation/whitespace)
    ///   (?:\r?\n)?                            → Optional line break (CRLF or LF)
    ///   $                                     → Anchor at the end of the string
    /// )                                       → End non-capturing group
    ///
    /// Notes:
    /// - Matches a full POTA code only at the start of the string.
    /// - Matches bare "POTA" only if it is the entire string.
    /// - Trailing whitespace, punctuation (. , ; |), and line breaks are stripped as part of the match.
    /// - Case-insensitive by RegexOptions.IgnoreCase.
    /// </summary>
    [GeneratedRegex(@"^(?:POTA\s+[A-Z]{2}-\d{4,5}\s*[.,;|]?\s*(?:\r?\n)?|POTA\s*[.,;|]?\s*(?:\r?\n)?$)", RegexOptions.IgnoreCase)]
    private static partial Regex PotaCommentRegex();

    /// <summary>
    /// Attempts to strip a leading POTA code (or just "POTA") from the input string.
    /// </summary>
    /// <param name="input">Input string (may be null).</param>
    /// <param name="foundCode">The matched POTA code, or null if none found.</param>
    /// <param name="result">The string with the POTA code removed, or unchanged if none found.</param>
    /// <returns>True if a POTA code was found and stripped; otherwise false.</returns>
    public static bool TryStripPotaCode(string? input, out string? foundCode, out string? result)
    {
        foundCode = null;
        result = input;

        if (string.IsNullOrWhiteSpace(input))
            return false;

        var match = PotaCommentRegex().Match(input);
        if (!match.Success)
            return false;

        foundCode = match.Value.Trim();
        result = PotaCommentRegex().Replace(input, string.Empty);

        return true;
    }

    /// <summary>
    /// Returns a representative frequency (in Hz) for a given band and mode.
    /// Uses the common FT8 frequency for the band when mode is FT8; otherwise returns the band's lower edge.
    /// </summary>
    /// <param name="band">Band string, e.g. "40M", "17M".</param>
    /// <param name="mode">Mode string, e.g. "FT8", "CW", "SSB".</param>
    /// <returns>Frequency in Hz, or null if the band is unrecognized.</returns>
    public static double? BandToFreqHz(string? band, string? mode)
    {
        if (string.IsNullOrWhiteSpace(band))
            return null;

        var isFt8 = string.Equals(mode, "FT8", StringComparison.OrdinalIgnoreCase);

        double? mhz = band.ToUpperInvariant() switch
        {
            "2190M" => isFt8 ? null    : 0.1357,
            "630M"  => isFt8 ? null    : 0.472,
            "160M"  => isFt8 ? null    : 1.8,
            "80M"   => isFt8 ? 3.573   : 3.5,
            "60M"   => isFt8 ? 5.357   : 5.3515,
            "40M"   => isFt8 ? 7.074   : 7.0,
            "30M"   => isFt8 ? 10.136  : 10.1,
            "20M"   => isFt8 ? 14.074  : 14.0,
            "17M"   => isFt8 ? 18.100  : 18.068,
            "15M"   => isFt8 ? 21.074  : 21.0,
            "12M"   => isFt8 ? 24.915  : 24.89,
            "10M"   => isFt8 ? 28.074  : 28.0,
            "6M"    => isFt8 ? 50.313  : 50.0,
            "4M"    => isFt8 ? 70.154  : 70.0,
            "2M"    => isFt8 ? 144.174 : 144.0,
            "1.25M" => isFt8 ? null    : 222.0,
            "70CM"  => isFt8 ? 432.174 : 420.0,
            "33CM"  => isFt8 ? null    : 902.0,
            "23CM"  => isFt8 ? 1296.174: 1240.0,
            _       => null
        };

        return mhz * 1_000_000;
    }
}
