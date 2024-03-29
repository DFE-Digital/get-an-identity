using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services;
using TeacherIdentity.AuthServer.Services.UserVerification;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Register;

public class ExistingAccountEmailConfirmationTests : TestBase, IAsyncLifetime
{
    private User? _existingUserAccount;

    public ExistingAccountEmailConfirmationTests(HostFixture hostFixture)
        : base(hostFixture)
    {
        HostFixture.RateLimitStore.Setup(x => x.IsClientIpBlockedForPinVerification(TestRequestClientIpProvider.ClientIpAddress)).ReturnsAsync(false);
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
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/existing-account-email-confirmation");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/existing-account-email-confirmation");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/existing-account-email-confirmation");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(_existingUserAccount), additionalScopes: null, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/existing-account-email-confirmation");
    }

    [Fact]
    public async Task Get_AccountNotConfirmed_RedirectsToAccountExists()
    {
        await GivenAuthenticationState_RedirectsTo(_previousPageAuthenticationState(_existingUserAccount), additionalScopes: null, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/existing-account-email-confirmation", "/sign-in/register/account-exists");
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(_existingUserAccount), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/register/existing-account-email-confirmation?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal(new Redactor().RedactEmail(authStateHelper.AuthenticationState.ExistingAccountEmail!), doc.GetElementByTestId("email")?.TextContent);
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/register/existing-account-email-confirmation");
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/register/existing-account-email-confirmation");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, trnRequirementType: null, HttpMethod.Post, "/sign-in/register/existing-account-email-confirmation");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(_existingUserAccount), additionalScopes: null, trnRequirementType: null, HttpMethod.Post, "/sign-in/register/existing-account-email-confirmation");
    }

    [Fact]
    public async Task Post_AccountNotConfirmed_RedirectsToAccountExists()
    {
        await GivenAuthenticationState_RedirectsTo(_previousPageAuthenticationState(_existingUserAccount), additionalScopes: null, trnRequirementType: null, HttpMethod.Post, "/sign-in/register/existing-account-email-confirmation", "/sign-in/register/account-exists");
    }

    [Fact]
    public async Task Post_UnknownPin_ReturnsError()
    {
        // The real PIN generation service never generates pins that start with a '0'
        var pin = "01234";

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(_existingUserAccount), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/existing-account-email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pin }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Code", "Enter a correct security code");
    }

    [Fact]
    public async Task Post_PinTooShort_ReturnsError()
    {
        // Arrange
        var pin = "0";

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(_existingUserAccount), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/existing-account-email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pin }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Code", "You’ve not entered enough numbers, the code must be 5 numbers");
    }

    [Fact]
    public async Task Post_PinTooLong_ReturnsError()
    {
        // Arrange
        var pin = "0123345678";

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(_existingUserAccount), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/existing-account-email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pin }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Code", "You’ve entered too many numbers, the code must be 5 numbers");
    }

    [Fact]
    public async Task Post_NonNumericPin_ReturnsError()
    {
        // Arrange
        var pin = "abc";

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(_existingUserAccount), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/existing-account-email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pin }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Code", "The code must be 5 numbers");
    }

    [Fact]
    public async Task Post_PinExpiredLessThanTwoHoursAgo_ReturnsErrorAndSendsANewPin()
    {
        // Arrange
        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pinResult = await userVerificationService.GenerateEmailPin(_existingUserAccount!.EmailAddress);
        Clock.AdvanceBy(TimeSpan.FromHours(1));
        SpyRegistry.Get<IUserVerificationService>().Reset();

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(_existingUserAccount), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/existing-account-email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pinResult.Pin! }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Code", "The security code has expired. New code sent.");

        HostFixture.UserVerificationService.Verify(mock => mock.GenerateEmailPin(_existingUserAccount!.EmailAddress), Times.Once);
    }

    [Fact]
    public async Task Post_PinExpiredMoreThanTwoHoursAgo_ReturnsErrorAndDoesNotSendANewPin()
    {
        // Arrange
        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var userVerificationOptions = HostFixture.Services.GetRequiredService<IOptions<UserVerificationOptions>>();
        var pinResult = await userVerificationService.GenerateEmailPin(_existingUserAccount!.EmailAddress);
        Clock.AdvanceBy(TimeSpan.FromHours(2) + TimeSpan.FromSeconds(userVerificationOptions.Value.PinLifetimeSeconds));
        SpyRegistry.Get<IUserVerificationService>().Reset();

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(_existingUserAccount), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/existing-account-email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pinResult.Pin! }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Code", "Enter a correct security code");

        HostFixture.UserVerificationService.Verify(mock => mock.GenerateEmailPin(_existingUserAccount!.EmailAddress), Times.Never);
    }

    [Fact]
    public async Task Post_ValidPin_UpdatesAuthenticationState()
    {
        // Arrange
        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pinResult = await userVerificationService.GenerateEmailPin(_existingUserAccount!.EmailAddress);

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(_existingUserAccount), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/existing-account-email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pinResult.Pin! }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);

        Assert.True(authStateHelper.AuthenticationState.EmailAddressVerified);
    }

    [Fact]
    public async Task Post_BlockedClient_ReturnsTooManyRequestsStatusCode()
    {
        // Arrange
        HostFixture.RateLimitStore.Setup(x => x.IsClientIpBlockedForPinVerification(TestRequestClientIpProvider.ClientIpAddress)).ReturnsAsync(true);

        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pinResult = await userVerificationService.GenerateEmailPin(_existingUserAccount!.EmailAddress);

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(_existingUserAccount), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/existing-account-email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pinResult.Pin! }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status429TooManyRequests, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidPinForNonAdminScopeWithNonAdminUser_UpdatesAuthenticationStateSignsInAndRedirects()
    {
        // Arrange
        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pinResult = await userVerificationService.GenerateEmailPin(_existingUserAccount!.EmailAddress);

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(_existingUserAccount), additionalScopes: CustomScopes.DefaultUserTypesScopes.First());
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/existing-account-email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pinResult.Pin! }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.AuthenticationState.PostSignInUrl, response.Headers.Location?.OriginalString);

        AssertAuthenticationStateUpdated(_existingUserAccount, authStateHelper);
    }

    private void AssertAuthenticationStateUpdated(User existingUser, AuthenticationStateHelper authStateHelper)
    {
        Assert.Equal(authStateHelper.AuthenticationState.UserId, existingUser.UserId);
        Assert.Equal(authStateHelper.AuthenticationState.EmailAddress, existingUser.EmailAddress);
        Assert.Equal(authStateHelper.AuthenticationState.MobileNumber, existingUser.MobileNumber);
        Assert.Equal(authStateHelper.AuthenticationState.FirstName, existingUser.FirstName);
        Assert.Equal(authStateHelper.AuthenticationState.LastName, existingUser.LastName);
        Assert.Equal(authStateHelper.AuthenticationState.DateOfBirth, existingUser.DateOfBirth);
        Assert.Equal(authStateHelper.AuthenticationState.Trn, existingUser.Trn);
        Assert.Equal(authStateHelper.AuthenticationState.UserType, existingUser.UserType);
        Assert.Equal(authStateHelper.AuthenticationState.StaffRoles, existingUser.StaffRoles);
        Assert.Equal(authStateHelper.AuthenticationState.TrnLookupStatus, existingUser.TrnLookupStatus);

        Assert.True(authStateHelper.AuthenticationState.EmailAddressVerified);
    }

    private readonly AuthenticationStateConfigGenerator _currentPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.ExistingAccountEmailConfirmation);
    private readonly AuthenticationStateConfigGenerator _previousPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.AccountExists);
}
