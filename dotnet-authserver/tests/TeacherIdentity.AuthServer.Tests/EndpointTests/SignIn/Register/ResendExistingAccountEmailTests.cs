using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Register;

public class ResendExistingAccountEmailTests : TestBase, IAsyncLifetime
{
    private User? _existingUserAccount;

    public ResendExistingAccountEmailTests(HostFixture hostFixture)
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
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/resend-existing-account-email");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/resend-existing-account-email");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, HttpMethod.Get, "/sign-in/register/resend-existing-account-email");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(ConfigureValidAuthenticationState, additionalScopes: null, HttpMethod.Get, "/sign-in/register/resend-existing-account-email");
    }

    [Fact]
    public async Task Get_ExistingAccountChosenNotSet_RedirectsToCheckAccount()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.RegisterExistingUserAccountMatch(), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/register/resend-existing-account-email?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/register/account-exists", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        await ValidRequest_RendersContent(ConfigureValidAuthenticationState, "/sign-in/register/resend-existing-account-email", additionalScopes: null);
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/register/resend-existing-account-email");
    }

    [Fact]
    public async Task Post_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/register/resend-existing-account-email");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, HttpMethod.Post, "/sign-in/register/resend-existing-account-email");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(ConfigureValidAuthenticationState, additionalScopes: null, HttpMethod.Post, "/sign-in/register/resend-existing-account-email");
    }

    [Fact]
    public async Task Post_ValidEmailWithBlockedClient_ReturnsTooManyRequestsStatusCode()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, additionalScopes: null);

        HostFixture.RateLimitStore.Setup(x => x.IsClientIpBlockedForPinGeneration(TestRequestClientIpProvider.ClientIpAddress)).ReturnsAsync(true);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/resend-existing-account-email?{authStateHelper.ToQueryParam()}")
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
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, additionalScopes: null);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/resend-existing-account-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/sign-in/register/existing-account-email-confirmation?{authStateHelper.ToQueryParam()}", response.Headers.Location?.OriginalString);

        HostFixture.UserVerificationService.Verify(mock => mock.GenerateEmailPin(_existingUserAccount!.EmailAddress), Times.Once);
    }

    private Func<AuthenticationState, Task> ConfigureValidAuthenticationState(AuthenticationStateHelper.Configure configure) =>
        configure.RegisterExistingUserAccountChosen(existingUserAccount: _existingUserAccount);
}
