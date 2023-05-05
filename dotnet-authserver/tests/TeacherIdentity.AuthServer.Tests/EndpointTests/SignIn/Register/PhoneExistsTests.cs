namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Register;

public class PhoneExistsTests : TestBase
{
    public PhoneExistsTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/phone-exists");
    }

    [Fact]
    public async Task Get_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/phone-exists");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        var user = await TestData.CreateUser();
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(user), additionalScopes: null, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/phone-exists");
    }

    [Fact]
    public async Task Get_UserNotSignedIn_RedirectsToPhoneConfirmation()
    {
        await GivenAuthenticationState_RedirectsTo(_previousPageAuthenticationState(), additionalScopes: null, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/phone-exists", "/sign-in/register/phone-confirmation");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_DoesNotRedirectToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_DoesNotRedirectToPostSignInUrl(additionalScopes: null, trnRequirementType: null, HttpMethod.Get, "/sign-in/complete");
    }

    [Fact]
    public async Task Get_ValidRequest_RendersContent()
    {
        var user = await TestData.CreateUser();
        await ValidRequest_RendersContent(_currentPageAuthenticationState(user), additionalScopes: null, trnRequirementType: null, url: "/sign-in/register/phone-exists");
    }

    [Fact]
    public async Task Post_UserNotSignedIn_RedirectsToPhoneConfirmation()
    {
        await GivenAuthenticationState_RedirectsTo(_previousPageAuthenticationState(), additionalScopes: null, trnRequirementType: null, HttpMethod.Post, "/sign-in/register/phone-exists", "/sign-in/register/phone-confirmation");
    }

    private readonly AuthenticationStateConfigGenerator _currentPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.PhoneExists);
    private readonly AuthenticationStateConfigGenerator _previousPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.PhoneConfirmation);
}
