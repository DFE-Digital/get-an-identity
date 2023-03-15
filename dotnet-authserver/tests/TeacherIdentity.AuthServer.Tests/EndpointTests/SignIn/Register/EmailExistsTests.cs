namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Register;

public class EmailExistsTests : TestBase
{
    public EmailExistsTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/email-exists");
    }

    [Fact]
    public async Task Get_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/email-exists");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        var user = await TestData.CreateUser();
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(user), additionalScopes: null, HttpMethod.Get, "/sign-in/register/email-exists");
    }

    [Fact]
    public async Task Get_UserNotSignedIn_RedirectsToEmailConfirmation()
    {
        await GivenAuthenticationState_RedirectsTo(_previousPageAuthenticationState(), HttpMethod.Get, "/sign-in/register/email-exists", "/sign-in/register/email-confirmation");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_DoesNotRedirectToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_DoesNotRedirectToPostSignInUrl(additionalScopes: null, HttpMethod.Get, "/sign-in/complete");
    }

    [Fact]
    public async Task Get_ValidRequest_RendersContent()
    {
        var user = await TestData.CreateUser();
        await ValidRequest_RendersContent(_currentPageAuthenticationState(user), "/sign-in/register/email-exists", additionalScopes: null);
    }

    [Fact]
    public async Task Post_UserNotSignedIn_RedirectsToEmailConfirmation()
    {
        await GivenAuthenticationState_RedirectsTo(_previousPageAuthenticationState(), HttpMethod.Post, "/sign-in/register/email-exists", "/sign-in/register/email-confirmation");
    }

    private readonly AuthenticationStateConfigGenerator _currentPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.EmailExists);
    private readonly AuthenticationStateConfigGenerator _previousPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.EmailConfirmation);
}
