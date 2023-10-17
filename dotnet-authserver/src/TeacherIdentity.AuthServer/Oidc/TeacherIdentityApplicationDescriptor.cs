using OpenIddict.Abstractions;
using TeacherIdentity.AuthServer.Models;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Oidc;

public class TeacherIdentityApplicationDescriptor : OpenIddictApplicationDescriptor
{
    private static readonly string[] _standardScopes = new[]
    {
        Scopes.Email,
        Scopes.Phone,
        Scopes.Profile
    }.ToArray();

    public static string[] StandardPermissions { get; } = new[]
    {
        OpenIddictConstants.Permissions.Endpoints.Authorization,
        OpenIddictConstants.Permissions.Endpoints.Token,
        OpenIddictConstants.Permissions.Endpoints.Logout,
        OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
        OpenIddictConstants.Permissions.ResponseTypes.Code
    }.Concat(StandardScopes.Select(sc => $"{OpenIddictConstants.Permissions.Prefixes.Scope}{sc}")).ToArray();

    public static string[] StandardScopes => _standardScopes;

    public string? ServiceUrl { get; set; }
    public TrnRequirementType TrnRequirementType { get; set; }
    public TrnMatchPolicy TrnMatchPolicy { get; set; }
    public bool BlockProhibitedTeachers { get; set; }
    public bool RaiseTrnResolutionSupportTickets { get; set; }

    public static TeacherIdentityApplicationDescriptor Create(
        string clientId,
        string clientSecret,
        string? displayName,
        string? serviceUrl,
        TrnRequirementType trnRequirementType,
        bool blockProhibitedTeachers,
        TrnMatchPolicy trnMatchProperty,
        bool raiseTrnResolutionSupportTickets,
        bool enableAuthorizationCodeGrant,
        bool enableClientCredentialsGrant,
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
            TrnRequirementType = trnRequirementType,
            TrnMatchPolicy = trnMatchProperty,
            BlockProhibitedTeachers = blockProhibitedTeachers,
            RaiseTrnResolutionSupportTickets = raiseTrnResolutionSupportTickets,
            Requirements =
            {
                OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange
            }
        };

        var permissions = new HashSet<string>();

        if (enableAuthorizationCodeGrant)
        {
            permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
            permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
            permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
            permissions.Add(OpenIddictConstants.Permissions.Endpoints.Logout);
            permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);
            permissions.Add(OpenIddictConstants.Permissions.Scopes.Email);
            permissions.Add(OpenIddictConstants.Permissions.Scopes.Phone);
            permissions.Add(OpenIddictConstants.Permissions.Scopes.Profile);

            foreach (var redirectUri in redirectUris)
            {
                descriptor.RedirectUris.Add(new Uri(redirectUri));
            }

            foreach (var redirectUri in postLogoutRedirectUris)
            {
                descriptor.PostLogoutRedirectUris.Add(new Uri(redirectUri));
            }
        }

        if (enableClientCredentialsGrant)
        {
            permissions.Add(OpenIddictConstants.Permissions.GrantTypes.ClientCredentials);
            permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
        }

        foreach (var permission in permissions)
        {
            descriptor.Permissions.Add(permission);
        }

        foreach (var scp in scopes)
        {
            descriptor.Permissions.Add($"{OpenIddictConstants.Permissions.Prefixes.Scope}{scp}");
        }

        return descriptor;
    }
}
