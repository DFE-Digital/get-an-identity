using Microsoft.AspNetCore.Authorization;

namespace TeacherIdentity.AuthServer.Infrastructure.Security;

/// <summary>
/// Defines a requirement that the calling client must have at least one of the specified scopes.
/// </summary>
public class ScopeAuthorizationRequirement : IAuthorizationRequirement
{
    public ScopeAuthorizationRequirement(string scope)
        : this(new[] { scope })
    {
    }

    public ScopeAuthorizationRequirement(params string[] scopes)
    {
        Scopes = scopes;
    }

    public string[] Scopes { get; }
}
