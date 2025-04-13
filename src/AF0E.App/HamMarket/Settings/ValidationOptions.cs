using FluentValidation;
using Microsoft.Extensions.Options;

namespace HamMarket.Settings;

public class ValidationOptions<T>(IValidator<T> validator) : IValidateOptions<T>
    where T : class
{
    public ValidateOptionsResult Validate(string? name, T options)
    {
        var result = validator.Validate(options);
        if (result.IsValid)
            return ValidateOptionsResult.Success;

        var errors = string.Join(", ", result.Errors.Select(e => e.ErrorMessage));
        return ValidateOptionsResult.Fail(errors);
    }
}
