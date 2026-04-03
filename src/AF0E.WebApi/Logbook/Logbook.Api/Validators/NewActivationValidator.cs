using System.Text.RegularExpressions;
using Logbook.Api.Requests;

namespace Logbook.Api.Validators;

public static partial class NewActivationValidator
{
    public static void ValidateAndThrow(NewActivationRequest req)
    {
        var errors = Validate(req);

        if (errors.Count > 0)
            throw new ArgumentException(string.Join("; ", errors), nameof(req));
    }

    private static List<string> Validate(NewActivationRequest req)
    {
        var errors = new List<string>();

        if (req.PrevDayActivationId is <= 0)
            errors.Add("Incorrect PrevDayActivationId");

        if (string.IsNullOrWhiteSpace(req.ParkNumber))
            errors.Add("ParkNum is required");
        else if (!ParkNumberRegex().IsMatch(req.ParkNumber.Trim()))
            errors.Add("ParkNum must be two letters, a dash, and 4-5 digits (e.g., US-1234)");

        if (string.IsNullOrWhiteSpace(req.Grid))
            errors.Add("Grid is required");
        else
        {
            var grid = req.Grid.Trim();
            if (grid.Length is not (4 or 6))
                errors.Add("Grid must be 4 or 6 characters long");
            else if (!GridRegex().IsMatch(grid))
                errors.Add("Grid format is invalid");
        }

        if (string.IsNullOrWhiteSpace(req.County))
            errors.Add("County is required");
        else if (req.County.Trim().Length > 200)
            errors.Add("County cannot exceed 200 characters");

        if (string.IsNullOrWhiteSpace(req.State))
            errors.Add("State is required");
        else if (!StateRegex().IsMatch(req.State.Trim()))
            errors.Add("State must be exactly 2 letters");

        if (req.Lat is < -90 or > 90)
            errors.Add("Lat must be between -90 and 90");

        if (req.Lon is < -180 or > 180)
            errors.Add("Lon must be between -180 and 180");

        return errors;
    }

    [GeneratedRegex(@"^[A-Za-z]{2}-\d{4,5}$")]
    private static partial Regex ParkNumberRegex();

    [GeneratedRegex(@"^[A-Ra-r]{2}\d{2}([A-Xa-x]{2})?$")]
    private static partial Regex GridRegex();

    [GeneratedRegex(@"^[A-Za-z]{2}$")]
    private static partial Regex StateRegex();
}
