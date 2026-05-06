using Logbook.Api.Requests;

namespace Logbook.Api.Validators;

public static class UpdateActivationValidator
{
    public static void ValidateAndThrow(UpdateActivationRequest req)
    {
        var errors = Validate(req);

        if (errors.Count > 0)
            throw new ArgumentException(string.Join("; ", errors), nameof(req));
    }

    private static List<string> Validate(UpdateActivationRequest req)
    {
        var errors = new List<string>();

        if (req.Id is <= 0)
            errors.Add("Incorrect ActivationId");

        ValidationRules.ValidateParkNumber(errors, req.ParkNum);

        ValidationRules.ValidateGrid(errors, req.Grid);
        ValidationRules.ValidateCounty(errors, req.County);
        ValidationRules.ValidateState(errors, req.State);
        ValidationRules.ValidateLatitude(errors, req.Lat);
        ValidationRules.ValidateLongitude(errors, req.Long);
        ValidationRules.ValidateCallSign(errors, req.StationCallsign);
        ValidationRules.ValidateCallSign(errors, req.OperatorCallsign);

        return errors;
    }
}
