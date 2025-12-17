using Microsoft.AspNetCore.Authorization;

namespace Logbook.Api.Security;

public static class AuthHelper
{
    public static async ValueTask<bool> HasPolicyAsync(
        string policy,
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user == null)
            return false;

        var result = await authorizationService.AuthorizeAsync(user, policy);
        return result.Succeeded;
    }

    public static async ValueTask<bool> HasAllPoliciesAsync(
        IEnumerable<string> policies,
        IAuthorizationService authorizationService,
        IHttpContextAccessor httpContextAccessor,
        bool requireAll = false)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user == null)
            return false;

        var anySucceeded = false;
        foreach (var policy in policies)
        {
            var result = await authorizationService.AuthorizeAsync(user, policy);
            switch (requireAll)
            {
                case true when !result.Succeeded:
                    return false;
                case false when result.Succeeded:
                    anySucceeded = true;
                    break;
            }
        }
        return requireAll || anySucceeded;
    }
}
