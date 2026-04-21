using System.Text.RegularExpressions;

namespace Logbook.Api.Validators;

internal static partial class ActivationValidationRules
{
    private const string ParkNumberFormatMessage = "ParkNum must be two letters, a dash, and 4-5 digits (e.g., US-1234)";

    internal static void ValidateParkNumber(List<string> errors, string? parkNumber, string fieldName = "ParkNum")
    {
        if (string.IsNullOrWhiteSpace(parkNumber))
        {
            errors.Add($"{fieldName} is required");
            return;
        }

        if (!ParkNumberRegex().IsMatch(parkNumber.Trim()))
            errors.Add(ParkNumberFormatMessage);
    }

    internal static void ValidateGrid(List<string> errors, string? grid)
    {
        if (string.IsNullOrWhiteSpace(grid))
        {
            errors.Add("Grid is required");
            return;
        }

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

    [GeneratedRegex(@"^[A-Za-z]{2}-\d{4,5}$")]
    private static partial Regex ParkNumberRegex();

    [GeneratedRegex(@"^[A-Ra-r]{2}\d{2}([A-Xa-x]{2})?$")]
    private static partial Regex GridRegex();

    [GeneratedRegex(@"^[A-Za-z]{2}$")]
    private static partial Regex StateRegex();
}
