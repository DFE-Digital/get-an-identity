using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

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
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.Trn, TrnRequirementType.Legacy, HttpMethod.Get, "/sign-in/trn");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(c => c.EmailVerified(), CustomScopes.Trn, TrnRequirementType.Legacy, HttpMethod.Get, "/sign-in/trn");
    }

    [Fact]
    public async Task Get_NoEmail_RedirectsToEmailPage()
    {
        await NoEmail_RedirectsToEmailPage(CustomScopes.Trn, TrnRequirementType.Legacy, HttpMethod.Get, "/sign-in/trn");
    }

    [Fact]
    public async Task Get_NoVerifiedEmail_RedirectsToEmailConfirmationPage()
    {
        await NoVerifiedEmail_RedirectsToEmailConfirmationPage(CustomScopes.Trn, TrnRequirementType.Legacy, HttpMethod.Get, "/sign-in/trn");
    }

    [Fact]
    public async Task Get_MatchedAgainstExistingUserWithTrn_RedirectsToTrnInUsePage()
    {
        // Arrange
        var existingUserWithTrn = await TestData.CreateUser(hasTrn: true);
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Trn.TrnLookupCompletedForExistingTrn(existingUserWithTrn), CustomScopes.Trn);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/different-email", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsOk()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified(), CustomScopes.Trn, trnRequirementType: TrnRequirementType.Legacy);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_WithRegisterForNpqClient_RendersCorrectContent()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified(), CustomScopes.Trn, TrnRequirementType.Legacy, TestClients.RegisterForNpq);

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
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified(), CustomScopes.Trn, TrnRequirementType.Legacy);

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
