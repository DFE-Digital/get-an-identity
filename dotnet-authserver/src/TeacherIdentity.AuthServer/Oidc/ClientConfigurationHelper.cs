using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace TeacherIdentity.AuthServer.Oidc;

public class ClientConfigurationHelper
{
    private readonly IServiceProvider _services;

    public ClientConfigurationHelper(IServiceProvider services)
    {
        _services = services;
    }

    public async Task UpsertClients(IEnumerable<ClientConfiguration> clients)
    {
        await using var scope = _services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        foreach (var clientConfig in clients)
        {
            var application = await manager.FindByClientIdAsync(clientConfig.ClientId ?? throw new Exception("Missing ClientId"));

            var descriptor = new TeacherIdentityApplicationDescriptor()
            {
                ClientId = clientConfig.ClientId,
                ClientSecret = clientConfig.ClientSecret,
                Type = ClientTypes.Confidential,
                ConsentType = ConsentTypes.Implicit,
                DisplayName = clientConfig.DisplayName,
                ServiceUrl = clientConfig.ServiceUrl,
                Requirements =
                {
                    Requirements.Features.ProofKeyForCodeExchange
                }
            };

            foreach (var redirectUri in clientConfig.RedirectUris ?? Array.Empty<string>())
            {
                descriptor.RedirectUris.Add(new Uri(redirectUri));
            }

            var permissions = new[]
            {
                Permissions.Endpoints.Authorization,
                Permissions.Endpoints.Token,
                Permissions.GrantTypes.AuthorizationCode,
                Permissions.ResponseTypes.Code,
                Permissions.Scopes.Email,
                Permissions.Scopes.Profile
            };

            foreach (var permission in permissions)
            {
                descriptor.Permissions.Add(permission);
            }

            foreach (var scp in clientConfig.Scopes ?? Array.Empty<string>())
            {
                descriptor.Permissions.Add($"scp:{scp}");
            }

            if (application is not null)
            {
                await manager.UpdateAsync(application, descriptor);
            }
            else
            {
                await manager.CreateAsync(descriptor);
            }
        }
    }
}
