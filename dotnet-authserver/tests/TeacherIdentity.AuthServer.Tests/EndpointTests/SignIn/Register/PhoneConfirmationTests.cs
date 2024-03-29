using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.UserVerification;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Register;

public class PhoneConfirmationTests : TestBase
{
    public PhoneConfirmationTests(HostFixture hostFixture)
        : base(hostFixture)
    {
        HostFixture.RateLimitStore.Setup(x => x.IsClientIpBlockedForPinVerification(TestRequestClientIpProvider.ClientIpAddress)).ReturnsAsync(false);
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/phone-confirmation");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/phone-confirmation");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/phone-confirmation");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(c => c.MobileNumberSet(), additionalScopes: null, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/phone-confirmation");
    }

    [Fact]
    public async Task Get_MobileNumberNotSet_RedirectsToPhonePage()
    {
        await GivenAuthenticationState_RedirectsTo(_previousPageAuthenticationState(), additionalScopes: null, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/phone-confirmation", "/sign-in/register/phone");
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.MobileNumberSet(), additionalScopes: null, trnRequirementType: null);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/register/phone-confirmation?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal(authStateHelper.AuthenticationState.MobileNumber, doc.GetElementByTestId("mobileNumber")?.TextContent);
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/register/phone-confirmation");
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/register/phone-confirmation");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, trnRequirementType: null, HttpMethod.Post, "/sign-in/register/phone-confirmation");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(c => c.MobileNumberSet(), additionalScopes: null, trnRequirementType: null, HttpMethod.Post, "/sign-in/register/phone-confirmation");
    }

    [Fact]
    public async Task Post_MobileNumberNotSet_RedirectsToPhonePage()
    {
        await GivenAuthenticationState_RedirectsTo(_previousPageAuthenticationState(), additionalScopes: null, trnRequirementType: null, HttpMethod.Post, "/sign-in/register/phone-confirmation", "/sign-in/register/phone");
    }

    [Fact]
    public async Task Post_UnknownPin_ReturnsError()
    {
        // Arrange
        var mobileNumber = TestData.GenerateUniqueMobileNumber();

        // The real PIN generation service never generates pins that start with a '0'
        var pin = "01234";

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(mobileNumber: mobileNumber), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/phone-confirmation?{authStateHelper.ToQueryParam()}")
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
        var mobileNumber = TestData.GenerateUniqueMobileNumber();
        var pin = "0";

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(mobileNumber: mobileNumber), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/phone-confirmation?{authStateHelper.ToQueryParam()}")
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
        var mobileNumber = TestData.GenerateUniqueMobileNumber();
        var pin = "0123345678";

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(mobileNumber: mobileNumber), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/phone-confirmation?{authStateHelper.ToQueryParam()}")
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
        var mobileNumber = TestData.GenerateUniqueMobileNumber();
        var pin = "abc";

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(mobileNumber: mobileNumber), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/phone-confirmation?{authStateHelper.ToQueryParam()}")
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
        var mobileNumber = TestData.GenerateUniqueMobileNumber();

        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pinResult = await userVerificationService.GenerateSmsPin(MobileNumber.Parse(mobileNumber));
        Clock.AdvanceBy(TimeSpan.FromHours(1));
        SpyRegistry.Get<IUserVerificationService>().Reset();

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(mobileNumber: mobileNumber), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/phone-confirmation?{authStateHelper.ToQueryParam()}")
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

        HostFixture.UserVerificationService.Verify(mock => mock.GenerateSmsPin(MobileNumber.Parse(mobileNumber)), Times.Once);
    }

    [Fact]
    public async Task Post_PinExpiredMoreThanTwoHoursAgo_ReturnsErrorAndDoesNotSendANewPin()
    {
        // Arrange
        var mobileNumber = TestData.GenerateUniqueMobileNumber();

        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var userVerificationOptions = HostFixture.Services.GetRequiredService<IOptions<UserVerificationOptions>>();
        var pinResult = await userVerificationService.GenerateSmsPin(MobileNumber.Parse(mobileNumber));
        Clock.AdvanceBy(TimeSpan.FromHours(2) + TimeSpan.FromSeconds(userVerificationOptions.Value.PinLifetimeSeconds));
        SpyRegistry.Get<IUserVerificationService>().Reset();

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(mobileNumber: mobileNumber), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/phone-confirmation?{authStateHelper.ToQueryParam()}")
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

        HostFixture.UserVerificationService.Verify(mock => mock.GenerateSmsPin(MobileNumber.Parse(mobileNumber)), Times.Never);
    }

    [Fact]
    public async Task Post_ValidPinForNewUser_UpdatesAuthenticationStateAndRedirects()
    {
        // Arrange
        var mobileNumber = TestData.GenerateUniqueMobileNumber();

        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pinResult = await userVerificationService.GenerateSmsPin(MobileNumber.Parse(mobileNumber));

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(mobileNumber: mobileNumber), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/phone-confirmation?{authStateHelper.ToQueryParam()}")
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
        Assert.StartsWith("/sign-in/register/name", response.Headers.Location?.OriginalString);

        Assert.True(authStateHelper.AuthenticationState.MobileNumberVerified);
    }

    [Fact]
    public async Task Post_BlockedClient_ReturnsTooManyRequestsStatusCode()
    {
        // Arrange
        HostFixture.RateLimitStore.Setup(x => x.IsClientIpBlockedForPinVerification(TestRequestClientIpProvider.ClientIpAddress)).ReturnsAsync(true);

        var mobileNumber = TestData.GenerateUniqueMobileNumber();
        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pinResult = await userVerificationService.GenerateSmsPin(MobileNumber.Parse(mobileNumber));

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(mobileNumber: mobileNumber), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/phone-confirmation?{authStateHelper.ToQueryParam()}")
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
        var user = await TestData.CreateUser(userType: Models.UserType.Default);

        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pinResult = await userVerificationService.GenerateSmsPin(MobileNumber.Parse(user.MobileNumber!));

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(mobileNumber: user.MobileNumber), additionalScopes: CustomScopes.DefaultUserTypesScopes.First());
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/phone-confirmation?{authStateHelper.ToQueryParam()}")
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
        Assert.StartsWith("/sign-in/register/phone-exists", response.Headers.Location?.OriginalString);

        Assert.True(authStateHelper.AuthenticationState.MobileNumberVerified);
        Assert.NotNull(authStateHelper.AuthenticationState.UserId);
    }

    [Fact]
    public async Task Post_ValidPinForNewUserAllQuestionsAnswered_RedirectsToCheckAnswers()
    {
        // Arrange
        var mobileNumber = MobileNumber.Parse(TestData.GenerateUniqueMobileNumber());

        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pinResult = await userVerificationService.GenerateSmsPin(mobileNumber);

        var authStateHelper = await CreateAuthenticationStateHelper(_allQuestionsAnsweredAuthenticationState(mobileNumber: mobileNumber.ToString()), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/phone-confirmation?{authStateHelper.ToQueryParam()}")
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
        Assert.StartsWith("/sign-in/register/check-answers", response.Headers.Location?.OriginalString);
    }

    private readonly AuthenticationStateConfigGenerator _currentPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.PhoneConfirmation);
    private readonly AuthenticationStateConfigGenerator _previousPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.Phone);
    private readonly AuthenticationStateConfigGenerator _allQuestionsAnsweredAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.CheckAnswers);
}
