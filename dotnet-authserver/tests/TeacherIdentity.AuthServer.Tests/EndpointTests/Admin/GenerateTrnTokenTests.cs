using TeacherIdentity.AuthServer.Events;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin;

public class GenerateTrnTokenTests : TestBase
{
    public GenerateTrnTokenTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_UnauthenticatedUser_RedirectsToSignIn()
    {
        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, "/admin/trn-tokens/new");
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Get, "/admin/trn-tokens/new");
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/trn-tokens/new");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_UnauthenticatedUser_RedirectsToSignIn()
    {
        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Post, "/admin/trn-tokens/new");
    }

    [Fact]
    public async Task Post_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Post, "/admin/trn-tokens/new");
    }

    [Fact]
    public async Task Post_MissingEmail_RendersError()
    {
        // Arrange
        var email = string.Empty;
        var trn = TestData.GenerateTrn();

        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/trn-tokens/new")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", email },
                { "Trn", trn }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();
        AssertEx.HtmlDocumentHasError(doc, "Email", "Enter the email address");
    }

    [Fact]
    public async Task Post_MissingTrn_RendersError()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var trn = string.Empty;

        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/trn-tokens/new")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", email },
                { "Trn", trn }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var doc = await response.GetDocument();
        AssertEx.HtmlDocumentHasError(doc, "Trn", "Enter the TRN");
    }

    [Fact]
    public async Task Post_ValidRequest_EmitsEventAndRendersToken()
    {
        // Arrange
        var userId = TestUsers.AdminUserWithAllRoles.UserId;
        HostFixture.SetUserId(userId);

        var email = Faker.Internet.Email();
        var trn = TestData.GenerateTrn();

        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/trn-tokens/new")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", email },
                { "Trn", trn }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
        var doc = await response.GetDocument();
        Assert.NotNull(doc.GetElementById("TrnToken")?.GetAttribute("value"));

        EventObserver.AssertEventsSaved(e =>
        {
            var trnTokenAddedEvent = Assert.IsType<TrnTokenAddedEvent>(e);
            Assert.Equal(Clock.UtcNow, trnTokenAddedEvent.CreatedUtc);
            Assert.True(trnTokenAddedEvent.ExpiresUtc > Clock.UtcNow);
            Assert.Equal(email, trnTokenAddedEvent.Email);
            Assert.Equal(trn, trnTokenAddedEvent.Trn);
            Assert.Equal(userId, trnTokenAddedEvent.AddedByUserId);
        });
    }
}
