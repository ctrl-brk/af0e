using Logbook.Api.Requests;

namespace Logbook.Api.Validators;

public static class CopyActivationValidator
{
    public static void ValidateAndThrow(CopyActivationRequest req)
    {
        var errors = Validate(req);

        if (errors.Count > 0)
            throw new ArgumentException(string.Join("; ", errors), nameof(req));
    }

    private static List<string> Validate(CopyActivationRequest req)
    {
        var errors = new List<string>();

        ValidationRules.ValidateParkNumber(errors, req.ParkNumber);

        return errors;
    }
}
