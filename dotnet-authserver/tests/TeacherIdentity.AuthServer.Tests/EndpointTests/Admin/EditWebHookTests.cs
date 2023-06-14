using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Notifications.WebHooks;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin;

public class EditWebHookTests : TestBase
{


    public EditWebHookTests(HostFixture hostFixture)
        : base(hostFixture)
    {

    }

    [Fact]
    public async Task Get_UnauthenticatedUser_RedirectsToSignIn()
    {
        var webHookId = await CreateWebHook();

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, $"/admin/webhooks/{webHookId}");
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var webHookId = await CreateWebHook();

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Get, $"/admin/webhooks/{webHookId}");
    }

    [Fact]
    public async Task Get_WebHookDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var webHookId = Guid.NewGuid();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/webhooks/{webHookId}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var webHookId = await CreateWebHook();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/webhooks/{webHookId}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_EditWebHookWithinCacheTime_ShowsWarningMessage()
    {
        // Arrange
        var webHookId = await CreateWebHook(created: Clock.UtcNow, updated: Clock.UtcNow);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/webhooks/{webHookId}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();

        Assert.NotNull(doc.GetElementByTestId("warning-text"));

    }

    [Fact]
    public async Task Get_EditWebHookOutsideCacheTime_DoesNotShowWarningMessage()
    {
        // Arrange
        var webHookCreatedTime = Clock.UtcNow;
        var webHookCacheDuration = HostFixture.Services.GetRequiredService<IOptions<WebHookOptions>>().Value.WebHooksCacheDurationSeconds;

        var webHookUpdatedTIme = Clock.UtcNow.AddSeconds(-webHookCacheDuration - 20);

        var webHookId = await CreateWebHook(created: webHookCreatedTime, updated: webHookUpdatedTIme);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/webhooks/{webHookId}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();

        Assert.Null(doc.GetElementByTestId("warning-text"));

    }

    [Fact]
    public async Task Post_UnauthenticatedUser_RedirectsToSignIn()
    {
        var webHookId = await CreateWebHook();

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Post, $"/admin/webhooks/{webHookId}");
    }

    [Fact]
    public async Task Post_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var webHookId = await CreateWebHook();

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Post, $"/admin/webhooks/{webHookId}");
    }

    [Fact]
    public async Task Post_WebHookDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var webHookId = Guid.NewGuid();
        var endpoint = Faker.Internet.Url();
        var enabled = true;
        var webHookMessageTypes = WebHookMessageTypes.UserMerged;

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/webhooks/{webHookId}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Endpoint", endpoint },
                { "Enabled", enabled },
                { "WebHookMessageTypes", webHookMessageTypes },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidRequest_UpdatesWebHookEmitsEventAndRedirects()
    {
        // Arrange
        var webHookId = await CreateWebHook(enabled: false);
        var endpoint = Faker.Internet.Url();
        var enabled = true;
        var webHookMessageTypes = WebHookMessageTypes.UserMerged;

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/webhooks/{webHookId}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Endpoint", endpoint },
                { "Enabled", enabled },
                { "WebHookMessageTypes", webHookMessageTypes }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/admin/webhooks/{webHookId}", response.Headers.Location?.OriginalString);

        await TestData.WithDbContext(async dbContext =>
        {
            var webHook = await dbContext.WebHooks.SingleAsync(u => u.WebHookId == webHookId);

            Assert.Equal(endpoint, webHook.Endpoint);
            Assert.Equal(enabled, webHook!.Enabled);
            Assert.Equal(webHookMessageTypes, webHook.WebHookMessageTypes);
        });

        EventObserver.AssertEventsSaved(
            e =>
            {
                var webHookUpdated = Assert.IsType<WebHookUpdatedEvent>(e);
                Assert.Equal(TestUsers.AdminUserWithAllRoles.UserId, webHookUpdated.UpdatedByUserId);
                Assert.Equal(Clock.UtcNow, webHookUpdated.CreatedUtc);
                Assert.Equal(webHookId, webHookUpdated.WebHookId);
                Assert.Equal(enabled, webHookUpdated.Enabled);
                Assert.Equal(endpoint, webHookUpdated.Endpoint);
                Assert.Equal(webHookMessageTypes, webHookUpdated.WebHookMessageTypes);
                Assert.Equal(WebHookUpdatedEventChanges.Enabled | WebHookUpdatedEventChanges.Endpoint | WebHookUpdatedEventChanges.WebHookMessageTypes, webHookUpdated.Changes);
            });
    }

    [Fact]
    public async Task Post_ValidRequestWithRegenerateSecretFalse_DoesNotChangeSecret()
    {
        // Arrange
        var webHookId = await CreateWebHook(enabled: false);
        var endpoint = Faker.Internet.Url();
        var enabled = true;
        var webHookMessageTypes = WebHookMessageTypes.UserMerged;

        var originalSecret = (await TestData.WithDbContext(dbContext => dbContext.WebHooks.SingleAsync(u => u.WebHookId == webHookId))).Secret;

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/webhooks/{webHookId}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Endpoint", endpoint },
                { "Enabled", enabled },
                { "WebHookMessageTypes", webHookMessageTypes },
                { "RegenerateSecret", false.ToString() }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/admin/webhooks/{webHookId}", response.Headers.Location?.OriginalString);

        await TestData.WithDbContext(async dbContext =>
        {
            var webHook = await dbContext.WebHooks.SingleAsync(u => u.WebHookId == webHookId);

            Assert.Equal(originalSecret, webHook.Secret);
        });

        EventObserver.AssertEventsSaved(
            e =>
            {
                var webHookUpdated = Assert.IsType<WebHookUpdatedEvent>(e);
                Assert.False(webHookUpdated.Changes.HasFlag(WebHookUpdatedEventChanges.Secret));
            });
    }

    [Fact]
    public async Task Post_ValidRequestWithRegenerateSecretTrue_GeneratesNewSecret()
    {
        // Arrange
        var webHookId = await CreateWebHook(enabled: false);
        var endpoint = Faker.Internet.Url();
        var enabled = true;
        var webHookMessageTypes = WebHookMessageTypes.UserMerged;

        var originalSecret = (await TestData.WithDbContext(dbContext => dbContext.WebHooks.SingleAsync(u => u.WebHookId == webHookId))).Secret;

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/webhooks/{webHookId}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Endpoint", endpoint },
                { "Enabled", enabled },
                { "WebHookMessageTypes", webHookMessageTypes },
                { "RegenerateSecret", true.ToString() }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/admin/webhooks/{webHookId}", response.Headers.Location?.OriginalString);

        await TestData.WithDbContext(async dbContext =>
        {
            var webHook = await dbContext.WebHooks.SingleAsync(u => u.WebHookId == webHookId);

            Assert.NotEqual(originalSecret, webHook.Secret);
        });

        EventObserver.AssertEventsSaved(
            e =>
            {
                var webHookUpdated = Assert.IsType<WebHookUpdatedEvent>(e);
                Assert.True(webHookUpdated.Changes.HasFlag(WebHookUpdatedEventChanges.Secret));
            });
    }

    private Task<Guid> CreateWebHook(string? endpoint = null, bool enabled = true, DateTime? created = null, DateTime? updated = null) =>
        TestData.WithDbContext(async dbContext =>
        {
            var webHookId = Guid.NewGuid();

            endpoint ??= Faker.Internet.Url();

            created ??= Clock.UtcNow;
            updated ??= Clock.UtcNow;

            dbContext.WebHooks.Add(new WebHook()
            {
                Created = (DateTime)created,
                Updated = (DateTime)updated,
                Enabled = enabled,
                Endpoint = endpoint,
                WebHookId = webHookId,
                Secret = WebHook.GenerateSecret(),
                WebHookMessageTypes = WebHookMessageTypes.UserUpdated
            });

            await dbContext.SaveChangesAsync();

            return webHookId;
        });
}
