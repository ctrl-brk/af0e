namespace HamMarket.Settings;

using FluentValidation;

public class SettingsValidator : AbstractValidator<AppSettings>
{
    public SettingsValidator()
    {
        RuleFor(x => x).Must(settings => settings.Email.Smtp.Enabled ^ settings.Email.SendGrid.Enabled).WithMessage("Either Smtp or SendGrid must be enabled but not both");

        When(x => x.Email.Smtp.Enabled, () =>
        {
            RuleFor(x => x.Email.Smtp.SmtpServer).NotEmpty().WithMessage("Smtp server address is required");
            RuleFor(x => x.Email.Smtp.User).NotEmpty().WithMessage("Smtp user is required");
            RuleFor(x => x.Email.Smtp.Password).NotEmpty().WithMessage("Smtp password is required");
        });

        When(x => x.Email.SendGrid.Enabled, () =>
        {
            RuleFor(x => x.Email.SendGrid.ApiKey).NotEmpty().WithMessage("SendGrid API key is required");
        });

        RuleFor(x => x.Email.From).NotEmpty().WithMessage("Email from address is required");
        RuleFor(x => x.Email.To).NotEmpty().WithMessage("Email to address is required");
    }
}
