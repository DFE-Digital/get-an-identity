using OpenIddict.Abstractions;
using TeacherIdentity.AuthServer.Oidc;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Tests;

public static class TestClients
{
    public static OpenIddictApplicationDescriptor[] All => new[] { Client1 };

    public static TeacherIdentityApplicationDescriptor Client1 { get; } = new TeacherIdentityApplicationDescriptor()
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
            Permissions.GrantTypes.Password,
            Permissions.ResponseTypes.Code,
            Permissions.Scopes.Email,
            Permissions.Scopes.Profile,
            $"{Permissions.Prefixes.Scope}{CustomScopes.DqtRead}",
            $"{Permissions.Prefixes.Scope}{CustomScopes.GetAnIdentitySupport}",
            $"{Permissions.Prefixes.Scope}{CustomScopes.UserRead}",
            $"{Permissions.Prefixes.Scope}{CustomScopes.UserWrite}",
            $"{Permissions.Prefixes.Scope}{CustomScopes.Trn}",
        },
        Requirements =
        {
            Requirements.Features.ProofKeyForCodeExchange
        }
    };
}
