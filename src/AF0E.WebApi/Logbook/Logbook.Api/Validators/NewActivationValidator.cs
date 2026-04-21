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

        ActivationValidationRules.ValidateParkNumber(errors, req.ParkNumber);

        ActivationValidationRules.ValidateGrid(errors, req.Grid);
        ActivationValidationRules.ValidateCounty(errors, req.County);
        ActivationValidationRules.ValidateState(errors, req.State);
        ActivationValidationRules.ValidateLatitude(errors, req.Lat);
        ActivationValidationRules.ValidateLongitude(errors, req.Lon);

        return errors;
    }
}
