using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Register;

public class ResendExistingAccountPhoneTests : TestBase, IAsyncLifetime
{
    private User? _existingUserAccount;

    public ResendExistingAccountPhoneTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    public async Task InitializeAsync()
    {
        _existingUserAccount = await TestData.CreateUser();
    }

    public Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/resend-existing-account-phone");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/resend-existing-account-phone");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, HttpMethod.Get, "/sign-in/register/resend-existing-account-phone");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(_existingUserAccount), additionalScopes: null, HttpMethod.Get, "/sign-in/register/resend-existing-account-phone");
    }

    [Fact]
    public async Task Get_NullExistingAccountMobileNumber_RedirectsToExistingAccountEmailConfirmation()
    {
        _existingUserAccount!.MobileNumber = null;
        await GivenAuthenticationState_RedirectsTo(_currentPageAuthenticationState(_existingUserAccount), HttpMethod.Get, "/sign-in/register/resend-existing-account-phone", "/sign-in/register/existing-account-email-confirmation");
    }

    [Fact]
    public async Task Get_ExistingAccountNotChosen_RedirectsToAccountExists()
    {
        await GivenAuthenticationState_RedirectsTo(_previousPageAuthenticationState(_existingUserAccount), HttpMethod.Get, "/sign-in/register/resend-existing-account-phone", "/sign-in/register/account-exists");
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        await ValidRequest_RendersContent(_currentPageAuthenticationState(_existingUserAccount), "/sign-in/register/resend-existing-account-phone", additionalScopes: null);
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/register/resend-existing-account-phone");
    }

    [Fact]
    public async Task Post_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/register/resend-existing-account-phone");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, HttpMethod.Post, "/sign-in/register/resend-existing-account-phone");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(_existingUserAccount), additionalScopes: null, HttpMethod.Post, "/sign-in/register/resend-existing-account-phone");
    }

    [Fact]
    public async Task Post_NullExistingAccountMobileNumber_RedirectsToExistingAccountEmailConfirmation()
    {
        _existingUserAccount!.MobileNumber = null;
        await GivenAuthenticationState_RedirectsTo(_currentPageAuthenticationState(_existingUserAccount), HttpMethod.Post, "/sign-in/register/resend-existing-account-phone", "/sign-in/register/existing-account-email-confirmation");
    }

    [Fact]
    public async Task Post_ExistingAccountNotChosen_RedirectsToAccountExists()
    {
        await GivenAuthenticationState_RedirectsTo(_previousPageAuthenticationState(_existingUserAccount), HttpMethod.Post, "/sign-in/register/resend-existing-account-phone", "/sign-in/register/account-exists");
    }

    [Fact]
    public async Task Post_ValidMobileNumberWithBlockedClient_ReturnsTooManyRequestsStatusCode()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(_existingUserAccount), additionalScopes: null);

        HostFixture.RateLimitStore.Setup(x => x.IsClientIpBlockedForPinGeneration(TestRequestClientIpProvider.ClientIpAddress)).ReturnsAsync(true);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/resend-existing-account-phone?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status429TooManyRequests, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidRequest_GeneratesPinAndRedirects()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(_existingUserAccount), additionalScopes: null);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/resend-existing-account-phone?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/sign-in/register/existing-account-phone-confirmation?{authStateHelper.ToQueryParam()}", response.Headers.Location?.OriginalString);

        HostFixture.UserVerificationService.Verify(mock => mock.GenerateSmsPin(_existingUserAccount!.MobileNumber!), Times.Once);
    }

    private readonly AuthenticationStateConfigGenerator _currentPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.ResendExistingAccountPhone);
    private readonly AuthenticationStateConfigGenerator _previousPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.AccountExists);
}
