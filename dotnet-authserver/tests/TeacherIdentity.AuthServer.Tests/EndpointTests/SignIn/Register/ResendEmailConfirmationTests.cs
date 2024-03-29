using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Register;

public class ResendEmailConfirmationTests : TestBase
{
    public ResendEmailConfirmationTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/resend-email-confirmation");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/resend-email-confirmation");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/resend-email-confirmation");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(c => c.EmailVerified(), additionalScopes: null, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/resend-email-confirmation");
    }

    [Fact]
    public async Task Get_EmailNotSet_RedirectsToEmailPage()
    {
        await GivenAuthenticationState_RedirectsTo(_previousPageAuthenticationState(), additionalScopes: null, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/resend-email-confirmation", "/sign-in/register/email");
    }

    [Fact]
    public async Task Get_EmailAlreadyVerified_RedirectsToRegisterPhone()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified(Faker.Internet.Email()), additionalScopes: null, trnRequirementType: null);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/register/resend-email-confirmation?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/sign-in/register/phone?{authStateHelper.ToQueryParam()}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/register/resend-email-confirmation?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal(authStateHelper.AuthenticationState.EmailAddress, doc.GetElementById("Email")?.GetAttribute("value"));
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/register/resend-email-confirmation");
    }

    [Fact]
    public async Task Post_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/register/resend-email-confirmation");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, trnRequirementType: null, HttpMethod.Post, "/sign-in/register/resend-email-confirmation");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(c => c.EmailVerified(), additionalScopes: null, trnRequirementType: null, HttpMethod.Post, "/sign-in/register/resend-email-confirmation");
    }

    [Fact]
    public async Task Post_EmailNotSet_RedirectsToEmailPage()
    {
        await GivenAuthenticationState_RedirectsTo(_previousPageAuthenticationState(), additionalScopes: null, trnRequirementType: null, HttpMethod.Post, "/sign-in/register/resend-email-confirmation", "/sign-in/register/email");
    }

    [Fact]
    public async Task Post_EmailAlreadyVerified_RedirectsToRegisterPhone()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified(), additionalScopes: null);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/resend-email-confirmation?{authStateHelper.ToQueryParam()}")
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
        Assert.Equal($"/sign-in/register/phone?{authStateHelper.ToQueryParam()}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_EmptyEmail_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);
        var differentEmail = "";

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/resend-email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", differentEmail }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Email", "Enter your email address");
    }

    [Fact]
    public async Task Post_InvalidEmail_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);
        var differentEmail = "xx";

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/resend-email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", differentEmail }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Email", "Enter a valid email address");
    }

    [Fact]
    public async Task Post_ValidEmailWithBlockedClient_ReturnsTooManyRequestsStatusCode()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);
        var differentEmail = "valid@email.com";

        HostFixture.RateLimitStore.Setup(x => x.IsClientIpBlockedForPinGeneration(TestRequestClientIpProvider.ClientIpAddress)).ReturnsAsync(true);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/resend-email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", differentEmail }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status429TooManyRequests, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidRequest_SetsEmailOnAuthenticationStateGeneratesPinAndRedirects()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);
        var differentEmail = Faker.Internet.Email();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/resend-email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", differentEmail }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/sign-in/register/email-confirmation?{authStateHelper.ToQueryParam()}", response.Headers.Location?.OriginalString);

        Assert.Equal(differentEmail, authStateHelper.AuthenticationState.EmailAddress);

        HostFixture.UserVerificationService.Verify(mock => mock.GenerateEmailPin(differentEmail), Times.Once);
    }

    [Fact]
    public async Task Post_ValidInstitutionEmail_SetsEmailOnAuthenticationStateGeneratesPinAndRedirectsToRegisterEmailConfirmation()
    {
        // Arrange
        var invalidEmailSuffix = "invalid.sch.uk";
        await TestData.EnsureEstablishmentDomain(invalidEmailSuffix);

        var email = $"headteacher@{invalidEmailSuffix}";

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", email }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/register/email-confirmation", response.Headers.Location?.OriginalString);

        Assert.Equal(email, authStateHelper.AuthenticationState.EmailAddress);
        Assert.True(authStateHelper.AuthenticationState.IsInstitutionEmail);

        HostFixture.UserVerificationService.Verify(mock => mock.GenerateEmailPin(email), Times.Once);
    }

    [Fact]
    public async Task Post_NotificationServiceInvalidEmail_ReturnsError()
    {
        // Arrange
        HostFixture.NotificationSender
            .Setup(mock => mock.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, string>>()))
            .Throws(new Exception("ValidationError"));

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);
        var email = Faker.Internet.Email();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/resend-email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", email }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Email", "Enter a valid email address");
    }

    private readonly AuthenticationStateConfigGenerator _currentPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.ResendEmail);
    private readonly AuthenticationStateConfigGenerator _previousPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.Email);
}
