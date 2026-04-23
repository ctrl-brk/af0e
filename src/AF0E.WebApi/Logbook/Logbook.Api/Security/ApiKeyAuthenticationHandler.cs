using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Logbook.Api.Security;

public static class ApiKeyAuthenticationDefaults
{
    public const string Scheme = "ApiKey";
}

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<ApiKeyAuthSettings> apiKeyOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var settings = apiKeyOptions.Value;

        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.Key) || string.IsNullOrWhiteSpace(settings.HeaderName) || !Request.Headers.TryGetValue(settings.HeaderName, out var providedKey) || string.IsNullOrWhiteSpace(providedKey))
            return Task.FromResult(AuthenticateResult.NoResult());

        if (!string.Equals(providedKey.ToString(), settings.Key, StringComparison.Ordinal))
            return Task.FromResult(AuthenticateResult.Fail("Invalid API key."));

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "adif-forwarder"),
            new Claim(ClaimTypes.Role, Roles.Admin)
        };

        var identity = new ClaimsIdentity(claims, ApiKeyAuthenticationDefaults.Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, ApiKeyAuthenticationDefaults.Scheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
