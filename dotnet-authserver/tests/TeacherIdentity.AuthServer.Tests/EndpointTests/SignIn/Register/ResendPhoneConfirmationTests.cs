using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Register;

public class ResendPhoneConfirmationTests : TestBase
{
    public ResendPhoneConfirmationTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/resend-phone-confirmation");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/resend-phone-confirmation");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, HttpMethod.Get, "/sign-in/register/resend-phone-confirmation");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(), additionalScopes: null, HttpMethod.Get, "/sign-in/register/resend-phone-confirmation");
    }

    [Fact]
    public async Task Get_PhoneNotSet_RedirectsToRegisterPhonePage()
    {
        await GivenAuthenticationState_RedirectsTo(_previousPageAuthenticationState(), HttpMethod.Get, "/sign-in/register/resend-phone-confirmation", "/sign-in/register/phone");
    }

    [Fact]
    public async Task Get_PhoneAlreadyVerified_RedirectsToRegisterName()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.MobileVerified(Faker.Internet.Email()), additionalScopes: null);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/register/resend-phone-confirmation?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/sign-in/register/name?{authStateHelper.ToQueryParam()}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        await ValidRequest_RendersContent(_currentPageAuthenticationState(), "/sign-in/register/resend-phone-confirmation", additionalScopes: null);
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/register/resend-phone-confirmation");
    }

    [Fact]
    public async Task Post_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/register/resend-phone-confirmation");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, HttpMethod.Post, "/sign-in/register/resend-phone-confirmation");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(), additionalScopes: null, HttpMethod.Post, "/sign-in/register/resend-phone-confirmation");
    }

    [Fact]
    public async Task Post_PhoneNotSet_RedirectsToRegisterPhonePage()
    {
        await GivenAuthenticationState_RedirectsTo(_previousPageAuthenticationState(), HttpMethod.Post, "/sign-in/register/resend-phone-confirmation", "/sign-in/register/phone");
    }

    [Fact]
    public async Task Post_PhoneAlreadyVerified_RedirectsToRegisterName()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.MobileVerified(), additionalScopes: null);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/resend-phone-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", email }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/sign-in/register/name?{authStateHelper.ToQueryParam()}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_EmptyMobileNumber_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/resend-phone-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "MobileNumber", "Enter your mobile phone number");
    }

    [Fact]
    public async Task Post_ValidMobileNumberWithBlockedClient_ReturnsTooManyRequestsStatusCode()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);

        HostFixture.RateLimitStore.Setup(x => x.IsClientIpBlockedForPinGeneration(TestRequestClientIpProvider.ClientIpAddress)).ReturnsAsync(true);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/resend-phone-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "MobileNumber", TestData.GenerateUniqueMobileNumber() }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status429TooManyRequests, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidRequest_SetsMobileNumberOnAuthenticationStateGeneratesPinAndRedirects()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);
        var mobileNumber = TestData.GenerateUniqueMobileNumber();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/resend-phone-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "MobileNumber", mobileNumber }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/sign-in/register/phone-confirmation?{authStateHelper.ToQueryParam()}", response.Headers.Location?.OriginalString);

        Assert.Equal(mobileNumber, authStateHelper.AuthenticationState.MobileNumber);

        HostFixture.UserVerificationService.Verify(mock => mock.GenerateSmsPin(MobileNumber.Parse(mobileNumber)), Times.Once);
    }

    [Fact]
    public async Task Post_InvalidNotificationServiceMobileNumber_ReturnsError()
    {
        // Arrange
        HostFixture.NotificationSender
            .Setup(mock => mock.SendSms(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Throws(new Exception("ValidationError"));

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);
        var mobileNumber = TestData.GenerateUniqueMobileNumber();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/resend-phone-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "MobileNumber", mobileNumber }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "MobileNumber", "Enter a valid mobile phone number");
    }

    private readonly AuthenticationStateConfigGenerator _currentPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.ResendPhone);
    private readonly AuthenticationStateConfigGenerator _previousPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.Phone);
}
