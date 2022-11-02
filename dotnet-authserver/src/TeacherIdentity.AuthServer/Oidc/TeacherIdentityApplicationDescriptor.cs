using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Oidc;

public class TeacherIdentityApplicationDescriptor : OpenIddictApplicationDescriptor
{
    private static readonly string[] _standardScopes = new[]
    {
        Scopes.Email,
        Scopes.Profile
    }.ToArray();

    public static string[] StandardPermissions { get; } = new[]
    {
        OpenIddictConstants.Permissions.Endpoints.Authorization,
        OpenIddictConstants.Permissions.Endpoints.Token,
        OpenIddictConstants.Permissions.Endpoints.Logout,
        OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
        OpenIddictConstants.Permissions.ResponseTypes.Code
    }.Concat(StandardScopes.Select(sc => $"scp:{sc}")).ToArray();

    public static string[] StandardScopes => _standardScopes;

    public string? ServiceUrl { get; set; }

    public static TeacherIdentityApplicationDescriptor Create(
        string clientId,
        string clientSecret,
        string? displayName,
        string? serviceUrl,
        IEnumerable<string> redirectUris,
        IEnumerable<string> postLogoutRedirectUris,
        IEnumerable<string> scopes)
    {
        var descriptor = new TeacherIdentityApplicationDescriptor()
        {
            ClientId = clientId,
            ClientSecret = clientSecret,
            Type = ClientTypes.Confidential,
            ConsentType = ConsentTypes.Implicit,
            DisplayName = displayName,
            ServiceUrl = serviceUrl,
            Requirements =
            {
                OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
            }
        };

        foreach (var redirectUri in redirectUris)
        {
            descriptor.RedirectUris.Add(new Uri(redirectUri));
        }

        foreach (var redirectUri in postLogoutRedirectUris)
        {
            descriptor.PostLogoutRedirectUris.Add(new Uri(redirectUri));
        }

        var permissions = new[]
        {
            OpenIddictConstants.Permissions.Endpoints.Authorization,
            OpenIddictConstants.Permissions.Endpoints.Token,
            OpenIddictConstants.Permissions.Endpoints.Logout,
            OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
            OpenIddictConstants.Permissions.ResponseTypes.Code,
            OpenIddictConstants.Permissions.Scopes.Email,
            OpenIddictConstants.Permissions.Scopes.Profile
        };

        foreach (var permission in permissions)
        {
            descriptor.Permissions.Add(permission);
        }

        foreach (var scp in scopes)
        {
            descriptor.Permissions.Add($"scp:{scp}");
        }

        return descriptor;
    }
}
