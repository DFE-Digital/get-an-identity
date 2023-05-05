using OpenIddict.Abstractions;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Tests;

public static class TestClients
{
    public static OpenIddictApplicationDescriptor[] All => new[] { DefaultClient, ApplyForQts, RegisterForNpq };

    public static TeacherIdentityApplicationDescriptor DefaultClient { get; } = new TeacherIdentityApplicationDescriptor()
    {
        ClientId = "default-client",
        ClientSecret = "secret",
        ConsentType = ConsentTypes.Implicit,
        DisplayName = "Test client app",
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
        },
        TrnRequirementType = TrnRequirementType.Optional
    };

    public static TeacherIdentityApplicationDescriptor ApplyForQts { get; } = new TeacherIdentityApplicationDescriptor()
    {
        ClientId = "apply-for-qts",
        ClientSecret = "secret",
        ConsentType = ConsentTypes.Implicit,
        DisplayName = "Apply for qualified teacher status (QTS) in England",
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
        },
        Requirements =
        {
            Requirements.Features.ProofKeyForCodeExchange
        }
    };

    public static TeacherIdentityApplicationDescriptor RegisterForNpq { get; } = new TeacherIdentityApplicationDescriptor()
    {
        ClientId = "register-for-npq",
        ClientSecret = "secret",
        ConsentType = ConsentTypes.Implicit,
        DisplayName = "Register for an NPQ",
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
        },
        Requirements =
        {
            Requirements.Features.ProofKeyForCodeExchange
        }
    };
}
