using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Register;

public class InstitutionEmailTests : TestBase
{
    public InstitutionEmailTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/institution-email");
    }

    [Fact]
    public async Task Get_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/register/institution-email");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/institution-email");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(), additionalScopes: null, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/institution-email");
    }

    [Fact]
    public async Task Get_ValidRequest_RendersContent()
    {
        await ValidRequest_RendersContent(_currentPageAuthenticationState(), additionalScopes: null, trnRequirementType: null, url: "/sign-in/register/institution-email");
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/register/institution-email");
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/register/institution-email");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, trnRequirementType: null, HttpMethod.Post, "/sign-in/register/institution-email");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(), additionalScopes: null, trnRequirementType: null, HttpMethod.Post, "/sign-in/register/institution-email");
    }

    [Fact]
    public async Task Post_EmptyUsePersonalEmail_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/institution-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "UsePersonalEmail", "Select which email address to use");
    }

    [Fact]
    public async Task Post_UseInstitutionEmail_UpdatesAuthenticationStateAndRedirects()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/institution-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "UsePersonalEmail", false },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/register/phone", response.Headers.Location?.OriginalString);

        Assert.True(authStateHelper.AuthenticationState.InstitutionEmailChosen);

        HostFixture.UserVerificationService.Verify(mock => mock.GenerateEmailPin(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Post_ValidPersonalEmailWithBlockedClient_ReturnsTooManyRequestsStatusCode()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);
        var email = Faker.Internet.Email();

        HostFixture.RateLimitStore.Setup(x => x.IsClientIpBlockedForPinGeneration(TestRequestClientIpProvider.ClientIpAddress)).ReturnsAsync(true);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/institution-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "UsePersonalEmail", true },
                { "PersonalEmailAddress", email }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status429TooManyRequests, (int)response.StatusCode);
    }


    [Fact]
    public async Task Post_EmptyPersonalEmail_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/institution-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "UsePersonalEmail", true },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "PersonalEmailAddress", "Enter a personal email address");
    }

    [Fact]
    public async Task Post_InvalidPersonalEmail_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/institution-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "UsePersonalEmail", true },
                { "PersonalEmailAddress", "xxx" },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "PersonalEmailAddress", "Enter an email address in the correct format, like name@example.com");
    }

    [Fact]
    public async Task Post_ValidPersonalEmail_SetsEmailOnAuthenticationStateGeneratesPinAndRedirectsToRegisterEmailConfirmation()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);
        var email = Faker.Internet.Email();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/institution-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "UsePersonalEmail", true },
                { "PersonalEmailAddress", email },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/register/email-confirmation", response.Headers.Location?.OriginalString);

        Assert.Equal(email, authStateHelper.AuthenticationState.EmailAddress);

        HostFixture.UserVerificationService.Verify(mock => mock.GenerateEmailPin(email), Times.Once);
    }

    [Fact]
    public async Task Post_AnotherInstitutionalEmail_SetsEmailOnAuthenticationStateGeneratesPinAndRedirectsToRegisterEmailConfirmation()
    {
        // Arrange
        var invalidEmailSuffix = "invalid.sch.uk";
        await TestData.EnsureEstablishmentDomain(invalidEmailSuffix);

        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);
        var email = "admin@invalid.sch.uk";

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/institution-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "UsePersonalEmail", true },
                { "PersonalEmailAddress", email },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/register/email-confirmation", response.Headers.Location?.OriginalString);

        Assert.Equal(email, authStateHelper.AuthenticationState.EmailAddress);

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

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/institution-email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "UsePersonalEmail", true },
                { "PersonalEmailAddress", email }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "PersonalEmailAddress", "Enter a valid email address");
    }

    private readonly AuthenticationStateConfigGenerator _currentPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.InstitutionEmail);
}
