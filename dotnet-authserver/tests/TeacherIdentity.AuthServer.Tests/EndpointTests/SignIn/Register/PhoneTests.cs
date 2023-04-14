using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Register;

public class PhoneTests : TestBase
{
    public PhoneTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/phone");
    }

    [Fact]
    public async Task Get_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/phone");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, HttpMethod.Get, "/sign-in/register/phone");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(), additionalScopes: null, HttpMethod.Get, "/sign-in/register/phone");
    }

    [Fact]
    public async Task Get_EmailNotVerified_RedirectsToEmailConfirmation()
    {
        await GivenAuthenticationState_RedirectsTo(_previousPageAuthenticationState(), HttpMethod.Get, "/sign-in/register/phone", "/sign-in/register/email-confirmation");
    }

    [Fact]
    public async Task Get_ValidRequest_RendersContent()
    {
        await ValidRequest_RendersContent(_currentPageAuthenticationState(), "/sign-in/register/phone", additionalScopes: null);
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/register/phone");
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/register/phone");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, HttpMethod.Post, "/sign-in/register/phone");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(), additionalScopes: null, HttpMethod.Post, "/sign-in/register/phone");
    }

    [Fact]
    public async Task Post_EmailNotVerified_RedirectsToEmailConfirmation()
    {
        await GivenAuthenticationState_RedirectsTo(_previousPageAuthenticationState(), HttpMethod.Post, "/sign-in/register/phone", "/sign-in/register/email-confirmation");
    }

    [Fact]
    public async Task Post_ValidMobileNumberWithBlockedClient_ReturnsTooManyRequestsStatusCode()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);
        var mobileNumber = TestData.GenerateUniqueMobileNumber();

        HostFixture.RateLimitStore.Setup(x => x.IsClientIpBlockedForPinGeneration(TestRequestClientIpProvider.ClientIpAddress)).ReturnsAsync(true);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/phone?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "MobileNumber", mobileNumber }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status429TooManyRequests, (int)response.StatusCode);
    }


    [Fact]
    public async Task Post_EmptyMobileNumber_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/phone?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "MobileNumber", "Enter your mobile phone number");
    }

    [Fact]
    public async Task Post_InvalidMobileNumber_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/phone?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "MobileNumber", "xxx" }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "MobileNumber", "Enter a valid mobile phone number");
    }

    [Fact]
    public async Task Post_ValidMobileNumber_SetsMobileNumberOnAuthenticationStateGeneratesPin()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);
        var mobileNumber = TestData.GenerateUniqueMobileNumber();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/phone?{authStateHelper.ToQueryParam()}")
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
        Assert.Equal(mobileNumber, authStateHelper.AuthenticationState.MobileNumber);

        var parsedMobileNumber = MobileNumber.Parse(mobileNumber);
        HostFixture.UserVerificationService.Verify(mock => mock.GenerateSmsPin(parsedMobileNumber), Times.Once);
    }

    [Fact]
    public async Task Post_NotificationServiceInvalidMobileNumber_ReturnsError()
    {
        // Arrange
        HostFixture.NotificationSender
            .Setup(mock => mock.SendSms(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Throws(new Exception("ValidationError"));

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);
        var mobileNumber = TestData.GenerateUniqueMobileNumber();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/phone?{authStateHelper.ToQueryParam()}")
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

    private readonly AuthenticationStateConfigGenerator _currentPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.Phone);
    private readonly AuthenticationStateConfigGenerator _previousPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.EmailConfirmation);
}
