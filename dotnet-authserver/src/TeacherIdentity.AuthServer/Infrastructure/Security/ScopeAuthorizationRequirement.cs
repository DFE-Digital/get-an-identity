using Microsoft.AspNetCore.Authorization;

namespace TeacherIdentity.AuthServer.Infrastructure.Security;

public class ScopeAuthorizationRequirement : IAuthorizationRequirement
{
    public ScopeAuthorizationRequirement(string scope)
    {
        Scope = scope;
    }

    public string Scope { get; }
}
