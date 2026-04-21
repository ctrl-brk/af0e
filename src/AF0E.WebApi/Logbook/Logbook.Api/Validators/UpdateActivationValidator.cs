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

        ActivationValidationRules.ValidateParkNumber(errors, req.ParkNum);

        ActivationValidationRules.ValidateGrid(errors, req.Grid);
        ActivationValidationRules.ValidateCounty(errors, req.County);
        ActivationValidationRules.ValidateState(errors, req.State);
        ActivationValidationRules.ValidateLatitude(errors, req.Lat);
        ActivationValidationRules.ValidateLongitude(errors, req.Long);

        return errors;
    }
}
