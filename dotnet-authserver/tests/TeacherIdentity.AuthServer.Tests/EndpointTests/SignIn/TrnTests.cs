using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.TrnLookup;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

public class TrnTests : TestBase
{
    public TrnTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.DqtRead, HttpMethod.Get, "/sign-in/trn");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(c => c.EmailVerified(), CustomScopes.DqtRead, HttpMethod.Get, "/sign-in/trn");
    }

    [Theory]
    [IncompleteAuthenticationMilestonesData(AuthenticationState.AuthenticationMilestone.EmailVerified)]
    public async Task Get_JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(
        AuthenticationState.AuthenticationMilestone milestone)
    {
        await JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(milestone, HttpMethod.Get, "/sign-in/trn");
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsOk()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified(), CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_WhenDqtReadScopeSpecified_SubmitsToTrnHasTrn()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified(), CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        var form = doc.GetElementsByTagName("form").Single();
        Assert.StartsWith("/sign-in/trn/has-trn", form.GetAttribute("action"));
        Assert.Equal("GET", form.GetAttribute("method"));
    }

    [Fact]
    public async Task Get_WhenDqtReadScopeNotSpecified_SubmitsToHandoverEndpoint()
    {
        // Arrange
#pragma warning disable CS0618 // Type or member is obsolete
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified(), CustomScopes.Trn);
#pragma warning restore CS0618 // Type or member is obsolete

        var handoverEndpoint = HostFixture.Services.GetRequiredService<IOptions<FindALostTrnIntegrationOptions>>().Value.HandoverEndpoint;

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        var form = doc.GetElementsByTagName("form").Single();
        Assert.Equal(handoverEndpoint, form.GetAttribute("action"));
        Assert.Equal("POST", form.GetAttribute("method"));
    }

    [Fact]
    public async Task Get_WithRegisterForNpqClient_RendersCorrectContent()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified(), CustomScopes.DqtRead, TestClients.RegisterForNpq);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Contains("Your details", doc.GetElementByTestId("trn-panel-content")!.InnerHtml);
        Assert.Contains("Before you register for an NPQ", doc.GetElementByTestId("trn-panel-content")!.InnerHtml);
    }

    [Fact]
    public async Task Get_WithDefaultClient_RendersCorrectContent()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified(), CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Contains("We’ll ask you some questions to check against our records", doc.GetElementByTestId("trn-panel-content")!.InnerHtml);
        Assert.Contains("We’ll ask for your:", doc.GetElementByTestId("trn-panel-content")!.InnerHtml);
    }
}
