using Microsoft.AspNetCore.Authorization;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Infrastructure.Security;

public class RequireScopeAuthorizationHandler : AuthorizationHandler<ScopeAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeAuthorizationRequirement requirement)
    {
        var scopeClaim = context.User.FindFirst(Claims.Scope)?.Value;

        if (string.IsNullOrEmpty(scopeClaim))
        {
            return Task.CompletedTask;
        }

        var scopes = scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (requirement.Scopes.Any(s => scopes.Contains(s)))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
