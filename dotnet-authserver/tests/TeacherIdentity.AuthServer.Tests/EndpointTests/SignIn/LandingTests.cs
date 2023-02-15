namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

public class LandingTests : TestBase
{
    public LandingTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/landing");
    }

    [Fact]
    public async Task Get_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/landing");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, HttpMethod.Get, "/sign-in/landing");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(c => c.Start(), additionalScopes: null, HttpMethod.Get, "/sign-in/landing");
    }

    [Fact]
    public async Task Get_ValidRequest_RendersContent()
    {
        await ValidRequest_RendersContent("/sign-in/landing", c => c.Start(), additionalScopes: null);
    }

    [Fact]
    public async Task Get_ValidRequestApplyForQTSClient_RendersClientScopedPartialContent()
    {
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Start(), additionalScopes: null, TestClients.ApplyForQts);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/landing?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();

        var doc = await response.GetDocument();
        Assert.Contains("Once you’ve created an account, you can continue to Apply for qualified teacher status (QTS) in England.", doc.GetElementByTestId("landing-content")!.InnerHtml);
    }
}
