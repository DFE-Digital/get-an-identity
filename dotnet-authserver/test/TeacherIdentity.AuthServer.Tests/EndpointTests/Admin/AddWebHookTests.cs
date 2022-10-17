using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin;

public class AddWebHookTests : TestBase
{
    public AddWebHookTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_UnauthenticatedUser_RedirectsToSignIn()
    {
        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, "/admin/webhooks/new");
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Get, "/admin/webhooks/new");
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/webhooks/new");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_UnauthenticatedUser_RedirectsToSignIn()
    {
        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Post, "/admin/webhooks/new");
    }

    [Fact]
    public async Task Post_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Post, "/admin/webhooks/new");
    }

    [Fact]
    public async Task Post_ValidRequest_CreatesWebHookEmitsEventAndRedirects()
    {
        // Arrange
        var endpoint = Faker.Internet.Url();
        var enabled = true;

        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/webhooks/new")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Endpoint", endpoint)
                .Add("Enabled", enabled)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);

        var webHookId = await TestData.WithDbContext(async dbContext =>
        {
            var webHook = await dbContext.WebHooks.SingleOrDefaultAsync(u => u.Endpoint == endpoint);

            Assert.NotNull(webHook);
            Assert.Equal(enabled, webHook!.Enabled);

            return webHook.WebHookId;
        });

        Assert.Equal($"/admin/webhooks/{webHookId}", response.Headers.Location?.OriginalString);

        EventObserver.AssertEventsSaved(
            e =>
            {
                var webHookAdded = Assert.IsType<WebHookAddedEvent>(e);
                Assert.Equal(TestUsers.AdminUserWithAllRoles.UserId, webHookAdded.AddedByUserId);
                Assert.Equal(Clock.UtcNow, webHookAdded.CreatedUtc);
                Assert.Equal(webHookId, webHookAdded.WebHookId);
                Assert.Equal(enabled, webHookAdded.Enabled);
                Assert.Equal(endpoint, webHookAdded.Endpoint);
            });
    }
}
