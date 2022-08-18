using OpenIddict.Abstractions;
using TeacherIdentity.AuthServer.Oidc;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Tests;

public static class TestClients
{
    public static OpenIddictApplicationDescriptor[] All => new[] { Client1 };

    public static OpenIddictApplicationDescriptor Client1 { get; } = new OpenIddictApplicationDescriptor()
    {
        ClientId = "testclient1",
        ClientSecret = "secret",
        ConsentType = ConsentTypes.Implicit,
        DisplayName = "Sample TeacherIdentity.TestClient app",
        RedirectUris =
        {
            new Uri("https://localhost:1234/oidc/callback")
        },
        Permissions =
        {
            Permissions.Endpoints.Authorization,
            Permissions.Endpoints.Token,
            Permissions.GrantTypes.AuthorizationCode,
            Permissions.ResponseTypes.Code,
            Permissions.Scopes.Email,
            Permissions.Scopes.Profile,
            $"scp:{CustomScopes.Trn}"
        },
        Requirements =
        {
            Requirements.Features.ProofKeyForCodeExchange
        }
    };
}
