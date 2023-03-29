using OpenIddict.Abstractions;

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

            var descriptor = TeacherIdentityApplicationDescriptor.Create(
                clientConfig.ClientId,
                clientConfig.ClientSecret,
                clientConfig.DisplayName,
                clientConfig.ServiceUrl,
                clientConfig.TrnRequirementType,
                clientConfig.EnableAuthorizationCodeGrant,
                clientConfig.EnableClientCredentialsGrant,
                clientConfig.RedirectUris ?? Array.Empty<string>(),
                clientConfig.PostLogoutRedirectUris ?? Array.Empty<string>(),
                clientConfig.Scopes ?? Array.Empty<string>());

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
