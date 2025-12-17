using System.Text.RegularExpressions;
using Logbook.Api.Models;

namespace Logbook.Api.Validators;

public static partial class QsoDetailsValidator
{
    // Valid modes
    private static readonly HashSet<string> _validModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "FT8", "CW", "SSB", "FM", "FT4", "MFSK", "PSK31", "JT65", "USB", "LSB", "AM", "RTTY", "PSK"
    };

    // Valid bands
    private static readonly HashSet<string> _validBands = new(StringComparer.OrdinalIgnoreCase)
    {
        "160m", "80m", "60m", "40m", "30m", "20m", "17m", "15m", "12m", "10m", "6m", "2m", "70cm",
        "1.25m", "23cm", "13cm", "9cm", "6cm", "3cm", "1.25cm", "6mm", "4mm", "2.5mm", "2mm", "1mm"
    };

    // Valid QSL status values
    private static readonly HashSet<string> _validQslStatus = new(StringComparer.OrdinalIgnoreCase)
    {
        "N", "V", "Q", "R", "Y", "I"
    };

    // Valid QSL via values
    private static readonly HashSet<string> _validQslVia = new(StringComparer.OrdinalIgnoreCase)
    {
        "B", "D", "E", "M"
    };

    /// <summary>
    /// Validates and throws ArgumentException if validation fails
    /// </summary>
    /// <param name="qso">The QsoDetails to validate</param>
    /// <exception cref="ArgumentException">Thrown when validation fails</exception>
    public static void ValidateAndThrow(QsoDetails qso)
    {
        var errors = Validate(qso);

        if (errors.Count > 0)
        {
            throw new ArgumentException(string.Join("; ", errors), nameof(qso));
        }
    }

    /// <summary>
    /// Validates QsoDetails for update operations
    /// </summary>
    /// <param name="qso">The QsoDetails to validate</param>
    /// <returns>List of validation error messages. Empty if valid.</returns>
    private static List<string> Validate(QsoDetails qso)
    {
        var errors = new List<string>();

        // Required fields
        if (string.IsNullOrWhiteSpace(qso.Call))
            errors.Add("Call sign is required");
        else
            ValidateCallSign(qso.Call, errors);

        if (string.IsNullOrWhiteSpace(qso.Band))
            errors.Add("Band is required");
        else if (!_validBands.Contains(qso.Band))
            errors.Add($"Invalid band: {qso.Band}");

        if (string.IsNullOrWhiteSpace(qso.Mode))
            errors.Add("Mode is required");
        else if (!_validModes.Contains(qso.Mode))
            errors.Add($"Invalid mode: {qso.Mode}");

        if (qso.Date == default)
            errors.Add("Date is required");
        else if (qso.Date > DateTime.UtcNow.AddDays(1))
            errors.Add("Date cannot be in the future");

        // Optional field validations
        if (qso.Freq.HasValue && qso.Freq.Value < 0)
            errors.Add("Frequency cannot be negative");

        if (qso.FreqRx.HasValue && qso.FreqRx.Value < 0)
            errors.Add("Receive frequency cannot be negative");

        if (!string.IsNullOrWhiteSpace(qso.RstSent))
            ValidateRst(qso.RstSent, "RST Sent", errors);

        if (!string.IsNullOrWhiteSpace(qso.RstRcvd))
            ValidateRst(qso.RstRcvd, "RST Received", errors);

        if (!string.IsNullOrWhiteSpace(qso.MyGrid))
            ValidateGridSquare(qso.MyGrid, errors);

        if (qso.MyCqZone.HasValue && (qso.MyCqZone.Value < 1 || qso.MyCqZone.Value > 40))
            errors.Add("CQ Zone must be between 1 and 40");

        if (qso.MyItuZone.HasValue && (qso.MyItuZone.Value < 1 || qso.MyItuZone.Value > 90))
            errors.Add("ITU Zone must be between 1 and 90");

        if (!string.IsNullOrWhiteSpace(qso.QslSent) && !_validQslStatus.Contains(qso.QslSent))
            errors.Add($"Invalid QSL Sent status: {qso.QslSent}");

        if (!string.IsNullOrWhiteSpace(qso.QslRcvd) && !_validQslStatus.Contains(qso.QslRcvd))
            errors.Add($"Invalid QSL Received status: {qso.QslRcvd}");

        if (!string.IsNullOrWhiteSpace(qso.QslSentVia) && !_validQslVia.Contains(qso.QslSentVia))
            errors.Add($"Invalid QSL Sent Via: {qso.QslSentVia}");

        if (!string.IsNullOrWhiteSpace(qso.QslRcvdVia) && !_validQslVia.Contains(qso.QslRcvdVia))
            errors.Add($"Invalid QSL Received Via: {qso.QslRcvdVia}");

        if (qso.QslSentDate.HasValue && qso.QslSentDate.Value > DateTime.UtcNow.AddDays(1))
            errors.Add("QSL Sent Date cannot be in the future");

        if (qso.QslRcvdDate.HasValue && qso.QslRcvdDate.Value > DateTime.UtcNow.AddDays(1))
            errors.Add("QSL Received Date cannot be in the future");

        if (!string.IsNullOrWhiteSpace(qso.SiteComment) && qso.SiteComment.Length > 64)
            errors.Add("Site Comment cannot exceed 64 characters");

        if (!string.IsNullOrWhiteSpace(qso.Comment) && qso.Comment.Length > 4000)
            errors.Add("Comment cannot exceed 4000 characters");

        return errors;
    }

    /// <summary>
    /// Validates a call sign according to amateur radio standards
    /// </summary>
    private static void ValidateCallSign(string callSign, List<string> errors)
    {
        // Must contain only letters, digits, and forward slashes
        if (!CallSignCharactersRegex().IsMatch(callSign))
        {
            errors.Add("Call sign can only contain letters, digits, and forward slashes");
            return;
        }

        // Split by forward slashes
        var parts = callSign.Split('/');

        // Can't have more than 2 slashes (3 parts max: prefix/call/suffix)
        if (parts.Length > 3)
        {
            errors.Add("Call sign can have at most 2 forward slashes");
            return;
        }

        // Can't have empty parts
        if (parts.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add("Call sign parts cannot be empty");
            return;
        }

        // Determine the main call sign
        var mainCallSign = parts.Length switch
        {
            1 => parts[0],
            2 => parts[0].Length >= parts[1].Length ? parts[0] : parts[1],
            _ => parts[1]
        };

        // Must be at least 3 characters long
        if (mainCallSign.Length < 3)
        {
            errors.Add("The main call sign must be at least 3 characters long");
            return;
        }

        // Must contain at least one digit
        if (!mainCallSign.Any(char.IsDigit))
        {
            errors.Add("The main call sign must contain at least one digit");
        }

        // Must contain at least one letter
        if (!mainCallSign.Any(char.IsAsciiLetter))
        {
            errors.Add("The main call sign must contain at least one letter");
        }
    }

    /// <summary>
    /// Validates RST (Readability-Strength-Tone) format
    /// RST must be 2-3 digits, optionally with +/- prefix (e.g., 59, +599, -73)
    /// </summary>
    private static void ValidateRst(string rst, string fieldName, List<string> errors)
    {
        if (!RstRegex().IsMatch(rst))
        {
            errors.Add($"{fieldName} must be 2-3 digits, optionally with +/- prefix (e.g., 59, +599, -73)");
        }
    }

    /// <summary>
    /// Validates Maidenhead grid square format
    /// Format: AA00 or AA00aa (e.g., DN70, DN70ab)
    /// </summary>
    private static void ValidateGridSquare(string grid, List<string> errors)
    {
        if (!GridSquareRegex().IsMatch(grid))
        {
            errors.Add("Invalid grid square format. Expected format: AA00 or AA00aa (e.g., DN70, DN70ab)");
        }
    }

    // Compiled regex patterns for better performance
    [GeneratedRegex(@"^[A-Za-z0-9\/]+$")]
    private static partial Regex CallSignCharactersRegex();

    [GeneratedRegex(@"^[+-]?[0-9]{2,3}$")]
    private static partial Regex RstRegex();

    [GeneratedRegex(@"^[A-R]{2}[0-9]{2}([A-X]{2})?$", RegexOptions.IgnoreCase)]
    private static partial Regex GridSquareRegex();
}
