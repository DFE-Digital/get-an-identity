using System.Security.Cryptography;
using OpenIddict.Abstractions;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin;

public class AddClientTests : TestBase
{
    public AddClientTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_UnauthenticatedUser_RedirectsToSignIn()
    {
        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, "/admin/clients/new");
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Get, "/admin/clients/new");
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/clients/new");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_UnauthenticatedUser_RedirectsToSignIn()
    {
        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Post, "/admin/clients/new");
    }

    [Fact]
    public async Task Post_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Post, "/admin/clients/new");
    }

    [Fact]
    public async Task Post_ClientAlreadyExistsWithId_RendersError()
    {
        // Arrange
        var clientId = TestClients.Client1.ClientId!;
        var clientSecret = "s3cret";
        var displayName = Faker.Company.Name();
        var serviceUrl = $"https://{Faker.Internet.DomainName()}/";
        var redirectUri1 = serviceUrl + "/callback";
        var redirectUri2 = serviceUrl + "/callback2";
        var postLogoutRedirectUri1 = serviceUrl + "/logout-callback";
        var postLogoutRedirectUri2 = serviceUrl + "/logout-callback2";
        var scope1 = CustomScopes.UserRead;
        var scope2 = CustomScopes.UserWrite;

        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/clients/new")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "ClientId", clientId },
                { "ClientSecret", clientSecret },
                { "DisplayName", displayName },
                { "ServiceUrl", serviceUrl },
                { "EnableAuthorizationCodeFlow", bool.TrueString },
                { "RedirectUris", string.Join("\n", new[] { redirectUri1, redirectUri2 }) },
                { "PostLogoutRedirectUris", string.Join("\n", new[] { postLogoutRedirectUri1, postLogoutRedirectUri2 }) },
                { "Scopes", scope1 },
                { "Scopes", scope2 }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "ClientId", "A client already exists with the specified client ID");
    }

    [Fact]
    public async Task Post_ValidRequest_CreatesClientEmitsEventAndRedirects()
    {
        // Arrange
        var clientId = GenerateRandomClientId();
        var clientSecret = "s3cret";
        var displayName = Faker.Company.Name();
        var serviceUrl = $"https://{Faker.Internet.DomainName()}/";
        var trnRequirementType = TrnRequirementType.Required;
        var redirectUri1 = serviceUrl + "/callback";
        var redirectUri2 = serviceUrl + "/callback2";
        var postLogoutRedirectUri1 = serviceUrl + "/logout-callback";
        var postLogoutRedirectUri2 = serviceUrl + "/logout-callback2";
        var scope1 = CustomScopes.UserRead;
        var scope2 = CustomScopes.UserWrite;

        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/clients/new")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "ClientId", clientId },
                { "ClientSecret", clientSecret },
                { "DisplayName", displayName },
                { "ServiceUrl", serviceUrl },
                { "TrnRequired", trnRequirementType == TrnRequirementType.Required },
                { "EnableAuthorizationCodeFlow", bool.TrueString },
                { "RedirectUris", string.Join("\n", new[] { redirectUri1, redirectUri2 }) },
                { "PostLogoutRedirectUris", string.Join("\n", new[] { postLogoutRedirectUri1, postLogoutRedirectUri2 }) },
                { "Scopes", scope1 },
                { "Scopes", scope2 }
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
        Assert.Equal(displayName, application.DisplayName);
        Assert.Equal(serviceUrl, application.ServiceUrl);
        Assert.Equal(trnRequirementType, application.TrnRequirementType);
        Assert.Collection(
            await applicationStore.GetRedirectUrisAsync(application, CancellationToken.None),
            uri => Assert.Equal(redirectUri1, uri),
            uri => Assert.Equal(redirectUri2, uri));
        Assert.Collection(
            await applicationStore.GetPostLogoutRedirectUrisAsync(application, CancellationToken.None),
            uri => Assert.Equal(postLogoutRedirectUri1, uri),
            uri => Assert.Equal(postLogoutRedirectUri2, uri));
        Assert.Collection(
            (await applicationStore.GetPermissionsAsync(application, CancellationToken.None))
                .Where(p => p.StartsWith(OpenIddictConstants.Permissions.Prefixes.Scope))
                .Select(p => p[OpenIddictConstants.Permissions.Prefixes.Scope.Length..])
                .Except(TeacherIdentityApplicationDescriptor.StandardScopes),
            sc => Assert.Equal(scope1, sc),
            sc => Assert.Equal(scope2, sc));

        EventObserver.AssertEventsSaved(
            e =>
            {
                var clientAdded = Assert.IsType<ClientAddedEvent>(e);
                Assert.Equal(TestUsers.AdminUserWithAllRoles.UserId, clientAdded.AddedByUserId);
                Assert.Equal(Clock.UtcNow, clientAdded.CreatedUtc);
                Assert.Equal(clientId, clientAdded.Client.ClientId);
                Assert.Equal(displayName, clientAdded.Client.DisplayName);
                Assert.Equal(serviceUrl, clientAdded.Client.ServiceUrl);
                Assert.Equal(trnRequirementType, clientAdded.Client.TrnRequirementType);
                Assert.Collection(
                    clientAdded.Client.RedirectUris,
                    uri => Assert.Equal(redirectUri1, uri),
                    uri => Assert.Equal(redirectUri2, uri));
                Assert.Collection(
                    clientAdded.Client.PostLogoutRedirectUris,
                    uri => Assert.Equal(postLogoutRedirectUri1, uri),
                    uri => Assert.Equal(postLogoutRedirectUri2, uri));
                Assert.Collection(
                    clientAdded.Client.Scopes.Except(TeacherIdentityApplicationDescriptor.StandardScopes),
                    sc => Assert.Equal(scope1, sc),
                    sc => Assert.Equal(scope2, sc));
            });

        var redirectedResponse = await response.FollowRedirect(HttpClient);
        var redirectedDoc = await redirectedResponse.GetDocument();
        AssertEx.HtmlDocumentHasFlashSuccess(redirectedDoc, "Client added");
    }

    private static string GenerateRandomClientId() => Convert.ToHexString(RandomNumberGenerator.GetBytes(12));
}
