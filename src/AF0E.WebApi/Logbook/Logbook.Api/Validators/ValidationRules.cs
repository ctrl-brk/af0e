using System.Text.RegularExpressions;

namespace Logbook.Api.Validators;

internal static partial class ValidationRules
{
    /// <summary>
    /// Validates a call sign according to amateur radio standards
    /// </summary>
    internal static void ValidateCallSign(List<string> errors, string? callSign, bool required = true)
    {
        switch (required)
        {
            case false when string.IsNullOrWhiteSpace(callSign):
                return;
            case true when string.IsNullOrWhiteSpace(callSign):
                errors.Add("Callsign is required");
                return;
        }

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

    private static readonly HashSet<string> _validBands = new(StringComparer.OrdinalIgnoreCase)
    {
        "160m", "80m", "60m", "40m", "30m", "20m", "17m", "15m", "12m", "10m", "6m", "2m", "70cm",
        "1.25m", "23cm", "13cm", "9cm", "6cm", "3cm", "1.25cm", "6mm", "4mm", "2.5mm", "2mm", "1mm"
    };
    internal static void ValidateBand(List<string> errors, string? band, bool required = true)
    {
        if (required && string.IsNullOrWhiteSpace(band))
            errors.Add("Band is required");
        else if (band != null && !_validBands.Contains(band))
            errors.Add($"Invalid band: {band}");
    }

    private static readonly HashSet<string> _validModes = new(StringComparer.OrdinalIgnoreCase)
    {
        "FT8", "CW", "SSB", "FM", "FT4", "FT2", "MFSK", "PSK31", "JT65", "USB", "LSB", "AM", "RTTY", "PSK"
    };
    internal static void ValidateQsoMode(List<string> errors, string? mode, bool required = true)
    {
        if (required && string.IsNullOrWhiteSpace(mode))
            errors.Add("Mode is required");
        else if (mode != null && !_validModes.Contains(mode))
            errors.Add($"Invalid mode: {mode}");
    }

    [GeneratedRegex(@"^(?:[0-9]{2,3}|[+-][0-9]{2})$")]
    private static partial Regex RstRegex();
    /// <summary>
    /// Validates RST (Readability-Strength-Tone) format.
    /// Unsigned RST values must be 2-3 digits; signed RST values must be +/- followed by exactly 2 digits (e.g., 59, 599, -10).
    /// </summary>
    internal static void ValidateRst(List<string> errors, string? rst, bool required = true)
    {
        if (required && string.IsNullOrWhiteSpace(rst))
        {
            errors.Add("RST is required");
            return;
        }

        if (!string.IsNullOrEmpty(rst) && !RstRegex().IsMatch(rst))
        {
            errors.Add("Invalid RST");
        }
    }

    internal static void ValidateGrid(List<string> errors, string? grid, bool required = true)
    {
        if (required && string.IsNullOrWhiteSpace(grid))
        {
            errors.Add("Grid is required");
            return;
        }

        if (string.IsNullOrWhiteSpace(grid))
            return;

        var trimmed = grid.Trim();
        if (trimmed.Length is not (4 or 6))
            errors.Add("Grid must be 4 or 6 characters long");
        else if (!GridRegex().IsMatch(trimmed))
            errors.Add("Grid format is invalid");
    }

    internal static void ValidateCounty(List<string> errors, string? county)
    {
        if (string.IsNullOrWhiteSpace(county))
            errors.Add("County is required");
        else if (county.Trim().Length > 200)
            errors.Add("County cannot exceed 200 characters");
    }

    internal static void ValidateState(List<string> errors, string? state)
    {
        if (string.IsNullOrWhiteSpace(state))
            errors.Add("State is required");
        else if (!StateRegex().IsMatch(state.Trim()))
            errors.Add("State must be exactly 2 letters");
    }

    internal static void ValidateLatitude(List<string> errors, decimal lat)
    {
        if (lat is < -90 or > 90)
            errors.Add("Lat must be between -90 and 90");
    }

    internal static void ValidateLongitude(List<string> errors, decimal lon)
    {
        if (lon is < -180 or > 180)
            errors.Add("Lon must be between -180 and 180");
    }

    private static readonly HashSet<string> _validQslStatus = new(StringComparer.OrdinalIgnoreCase)
    {
        "N", "V", "Q", "R", "Y", "I"
    };
    internal static void ValidateQslStatus(List<string> errors, string? status, bool required = true)
    {
        if (required && string.IsNullOrWhiteSpace(status))
            errors.Add("QSL status is required");
        else if (status != null && !_validQslStatus.Contains(status))
            errors.Add($"Invalid QSL status: {status}");
    }

    private static readonly HashSet<string> _validQslVia = new(StringComparer.OrdinalIgnoreCase)
    {
        "B", "D", "E", "M"
    };
    internal static void ValidateQslVia(List<string> errors, string? via, bool required = true)
    {
        if (required && string.IsNullOrWhiteSpace(via))
            errors.Add("QSL via is required");
        else if (via != null && !_validQslVia.Contains(via))
            errors.Add($"Invalid QSL Sent Via: {via}");
    }

    internal static void ValidateParkNumber(List<string> errors, string? parkNumber)
    {
        if (string.IsNullOrWhiteSpace(parkNumber))
        {
            errors.Add("Park number is required");
            return;
        }

        if (!ParkNumberRegex().IsMatch(parkNumber.Trim()))
            errors.Add("Park number must be two letters, a dash, and 4-5 digits (e.g., US-1234)");
    }

    [GeneratedRegex(@"^[A-Za-z0-9\/]+$")]
    private static partial Regex CallSignCharactersRegex();

    [GeneratedRegex(@"^[A-Za-z]{2}-\d{4,5}$")]
    private static partial Regex ParkNumberRegex();

    [GeneratedRegex(@"^[A-Ra-r]{2}\d{2}([A-Xa-x]{2})?$")]
    private static partial Regex GridRegex();

    [GeneratedRegex(@"^[A-Za-z]{2}$")]
    private static partial Regex StateRegex();
}
