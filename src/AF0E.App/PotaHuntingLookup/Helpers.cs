using System.Text.RegularExpressions;

namespace PotaHuntingLookup;

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
}
