using Logbook.Api.Requests;

namespace Logbook.Api.Validators;

public static class CloneActivationValidator
{
    public static void ValidateAndThrow(CloneActivationRequest req)
    {
        var errors = Validate(req);

        if (errors.Count > 0)
            throw new ArgumentException(string.Join("; ", errors), nameof(req));
    }

    private static List<string> Validate(CloneActivationRequest req)
    {
        var errors = new List<string>();

        ActivationValidationRules.ValidateParkNumber(errors, req.ParkNumber);

        return errors;
    }
}
