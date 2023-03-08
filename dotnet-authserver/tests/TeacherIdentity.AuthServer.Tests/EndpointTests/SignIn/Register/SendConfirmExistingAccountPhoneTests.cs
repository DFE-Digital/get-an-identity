using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Register;

[Collection(nameof(DisableParallelization))]  // Depends on mocks
public class SendConfirmExistingAccountPhoneTests : TestBase, IAsyncLifetime
{
    private User? _existingUserAccount;

    public SendConfirmExistingAccountPhoneTests(HostFixture hostFixture)
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
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/send-confirm-existing-account-phone");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/send-confirm-existing-account-phone");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, HttpMethod.Get, "/sign-in/register/send-confirm-existing-account-phone");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(ConfigureValidAuthenticationState, additionalScopes: null, HttpMethod.Get, "/sign-in/register/send-confirm-existing-account-phone");
    }

    [Fact]
    public async Task Get_PhoneNotKnown_RedirectsToCheckAccount()
    {
        // Arrange
        _existingUserAccount!.MobileNumber = null;

        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/register/send-confirm-existing-account-phone?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/sign-in/register/check-account?{authStateHelper.ToQueryParam()}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ExistingAccountChosenNotSet_RedirectsToCheckAccount()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.RegisterExistingUserAccountMatch(), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/register/resend-confirm-existing-account-email?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/register/check-account", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        await ValidRequest_RendersContent(ConfigureValidAuthenticationState, "/sign-in/register/send-confirm-existing-account-phone", additionalScopes: null);
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/register/send-confirm-existing-account-phone");
    }

    [Fact]
    public async Task Post_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/register/send-confirm-existing-account-phone");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, HttpMethod.Post, "/sign-in/register/send-confirm-existing-account-phone");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(ConfigureValidAuthenticationState, additionalScopes: null, HttpMethod.Post, "/sign-in/register/send-confirm-existing-account-phone");
    }

    [Fact]
    public async Task Post_PhoneNotKnown_RedirectsToCheckAccount()
    {
        // Arrange
        _existingUserAccount!.MobileNumber = null;

        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/send-confirm-existing-account-phone?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/sign-in/register/check-account?{authStateHelper.ToQueryParam()}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_ExistingAccountChosenNotSet_RedirectsToCheckAccount()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.RegisterExistingUserAccountMatch(), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/resend-confirm-existing-account-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/register/check-account", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_ValidRequest_GeneratesPinAndRedirects()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/send-confirm-existing-account-phone?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);

        HostFixture.UserVerificationService.Verify(mock => mock.GenerateSmsPin(_existingUserAccount!.MobileNumber!), Times.Once);
    }

    private Func<AuthenticationState, Task> ConfigureValidAuthenticationState(AuthenticationStateHelper.Configure configure) =>
        configure.RegisterExistingUserAccountChosen(_existingUserAccount);
}
