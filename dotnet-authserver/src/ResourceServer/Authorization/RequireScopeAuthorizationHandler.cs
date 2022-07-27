using Microsoft.AspNetCore.Authorization;

namespace ResourceServer.Authorization;

public class RequireScopeAuthorizationHandler : AuthorizationHandler<ScopeAuthorizationRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ScopeAuthorizationRequirement requirement)
    {
        var scopeClaim = context.User.FindFirst("scope")?.Value;

        if (string.IsNullOrEmpty(scopeClaim))
        {
            return Task.CompletedTask;
        }

        var scopes = scopeClaim.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (scopes.Any(s => s == requirement.Scope))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
