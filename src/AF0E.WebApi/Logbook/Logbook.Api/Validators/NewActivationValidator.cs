using Logbook.Api.Requests;

namespace Logbook.Api.Validators;

public static class NewActivationValidator
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

        ValidationRules.ValidateParkNumber(errors, req.ParkNumber);

        ValidationRules.ValidateGrid(errors, req.Grid);
        ValidationRules.ValidateCounty(errors, req.County);
        ValidationRules.ValidateState(errors, req.State);
        ValidationRules.ValidateLatitude(errors, req.Lat);
        ValidationRules.ValidateLongitude(errors, req.Lon);
        ValidationRules.ValidateCallSign(errors, req.StationCallsign);
        ValidationRules.ValidateCallSign(errors, req.OperatorCallsign);

        return errors;
    }
}
