using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin;

public class EditClientTests : TestBase
{
    public EditClientTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_UnauthenticatedUser_RedirectsToSignIn()
    {
        var clientId = await CreateClient();

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, $"/admin/clients/{clientId}");
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var clientId = await CreateClient();

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Get, $"/admin/clients/{clientId}");
    }

    [Fact]
    public async Task Get_ClientDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var clientId = Guid.NewGuid();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/clients/{clientId}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var clientId = await CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/clients/{clientId}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_UnauthenticatedUser_RedirectsToSignIn()
    {
        var clientId = await CreateClient();

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Post, $"/admin/clients/{clientId}");
    }

    [Fact]
    public async Task Post_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var clientId = await CreateClient();

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Post, $"/admin/clients/{clientId}");
    }

    [Fact]
    public async Task Post_ClientDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var clientId = Guid.NewGuid();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/clients/{clientId}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidRequest_UpdatesClientEmitsEventAndRedirects()
    {
        // Arrange
        var clientId = await CreateClient();

        var newClientSecret = "s3cret";
        var newDisplayName = Faker.Company.Name();
        var newServiceUrl = $"https://{Faker.Internet.DomainName()}/";
        var newRedirectUri1 = newServiceUrl + "/callback";
        var newRedirectUri2 = newServiceUrl + "/callback2";
        var newPostLogoutRedirectUri1 = newServiceUrl + "/logout-callback";
        var newPostLogoutRedirectUri2 = newServiceUrl + "/logout-callback2";
        var newScope = CustomScopes.GetAnIdentitySupport;

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/clients/{clientId}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "ResetClientSecret", bool.TrueString },
                { "ClientSecret", newClientSecret },
                { "DisplayName", newDisplayName },
                { "ServiceUrl", newServiceUrl },
                { "RedirectUris", string.Join("\n", new[] { newRedirectUri1, newRedirectUri2 }) },
                { "PostLogoutRedirectUris", string.Join("\n", new[] { newPostLogoutRedirectUri1, newPostLogoutRedirectUri2 }) },
                { "Scopes", newScope },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal("/admin/clients", response.Headers.Location?.OriginalString);

        using var scope = HostFixture.Services.CreateScope();
        var applicationStore = scope.ServiceProvider.GetRequiredService<TeacherIdentityApplicationStore>();
        var application = await applicationStore.FindByClientIdAsync(clientId, CancellationToken.None);

        Assert.NotNull(application);
        Assert.Equal(clientId, application!.ClientId);
        Assert.Equal(newDisplayName, application.DisplayName);
        Assert.Equal(newServiceUrl, application.ServiceUrl);
        Assert.Collection(
            await applicationStore.GetRedirectUrisAsync(application, CancellationToken.None),
            uri => Assert.Equal(newRedirectUri1, uri),
            uri => Assert.Equal(newRedirectUri2, uri));
        Assert.Collection(
            await applicationStore.GetPostLogoutRedirectUrisAsync(application, CancellationToken.None),
            uri => Assert.Equal(newPostLogoutRedirectUri1, uri),
            uri => Assert.Equal(newPostLogoutRedirectUri2, uri));
        Assert.Collection(
            (await applicationStore.GetPermissionsAsync(application, CancellationToken.None))
                .Where(p => p.StartsWith(OpenIddictConstants.Permissions.Prefixes.Scope))
                .Select(p => p[OpenIddictConstants.Permissions.Prefixes.Scope.Length..])
                .Except(TeacherIdentityApplicationDescriptor.StandardScopes),
            sc => Assert.Equal(newScope, sc));

        EventObserver.AssertEventsSaved(
            e =>
            {
                var clientUpdated = Assert.IsType<ClientUpdatedEvent>(e);
                Assert.Equal(TestUsers.AdminUserWithAllRoles.UserId, clientUpdated.UpdatedByUserId);
                Assert.Equal(Clock.UtcNow, clientUpdated.CreatedUtc);
                Assert.Equal(newDisplayName, clientUpdated.Client.DisplayName);
                Assert.Collection(
                    clientUpdated.Client.RedirectUris,
                    uri => Assert.Equal(newRedirectUri1, uri),
                    uri => Assert.Equal(newRedirectUri2, uri));
                Assert.Collection(
                    clientUpdated.Client.PostLogoutRedirectUris,
                    uri => Assert.Equal(newPostLogoutRedirectUri1, uri),
                    uri => Assert.Equal(newPostLogoutRedirectUri2, uri));
                Assert.Collection(
                    clientUpdated.Client.Scopes.Except(TeacherIdentityApplicationDescriptor.StandardScopes),
                    s => Assert.Equal(newScope, s));
                Assert.Equal(
                    ClientUpdatedEventChanges.ClientSecret |
                        ClientUpdatedEventChanges.DisplayName |
                        ClientUpdatedEventChanges.ServiceUrl |
                        ClientUpdatedEventChanges.RedirectUris |
                        ClientUpdatedEventChanges.PostLogoutRedirectUris |
                        ClientUpdatedEventChanges.Scopes,
                    clientUpdated.Changes);
            });
    }

    private async Task<string> CreateClient()
    {
        var clientId = RandomNumberGenerator.GetInt32(10000000, 99999999).ToString();
        var clientSecret = Guid.NewGuid().ToString();
        var displayName = Faker.Company.Name();
        var serviceUrl = $"https://{Faker.Internet.DomainName()}/";
        var redirectUri1 = serviceUrl + "/callback";
        var postLogoutRedirectUri1 = serviceUrl + "/logout-callback";
        var scope1 = CustomScopes.GetAnIdentityAdmin;

        var appManager = HostFixture.Services.GetRequiredService<TeacherIdentityApplicationManager>();
        await appManager.CreateAsync(
            TeacherIdentityApplicationDescriptor.Create(
                clientId,
                clientSecret,
                displayName,
                serviceUrl,
                new[] { redirectUri1 },
                new[] { postLogoutRedirectUri1 },
                new[] { scope1 }));

        return clientId;
    }
}
