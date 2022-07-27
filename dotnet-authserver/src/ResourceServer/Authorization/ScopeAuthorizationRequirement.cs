using Microsoft.AspNetCore.Authorization;

namespace ResourceServer.Authorization;

public class ScopeAuthorizationRequirement : IAuthorizationRequirement
{
    public ScopeAuthorizationRequirement(string scope)
    {
        Scope = scope;
    }

    public string Scope { get; }
}
