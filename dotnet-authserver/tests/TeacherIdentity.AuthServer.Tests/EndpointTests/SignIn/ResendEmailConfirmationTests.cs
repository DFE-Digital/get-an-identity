using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

[Collection(nameof(DisableParallelization))]  // Depends on mocks
public class ResendEmailConfirmationTests : TestBase
{
    public ResendEmailConfirmationTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/resend-email-confirmation");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/resend-email-confirmation");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, HttpMethod.Get, "/sign-in/resend-email-confirmation");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(c => c.EmailVerified(), additionalScopes: null, HttpMethod.Get, "/sign-in/resend-email-confirmation");
    }

    [Fact]
    public async Task Get_EmailNotKnown_RedirectsToEmailPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Start(), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/resend-email-confirmation?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/sign-in/email?{authStateHelper.ToQueryParam()}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_EmailAlreadyVerified_RedirectsToNextPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified(), additionalScopes: null);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/resend-email-confirmation?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet(), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/resend-email-confirmation?{authStateHelper.ToQueryParam()}");

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
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/resend-email-confirmation");
    }

    [Fact]
    public async Task Post_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/resend-email-confirmation");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, HttpMethod.Post, "/sign-in/resend-email-confirmation");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(c => c.EmailVerified(), additionalScopes: null, HttpMethod.Post, "/sign-in/resend-email-confirmation");
    }

    [Fact]
    public async Task Post_EmailNotKnown_RedirectsToEmailPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Start(), additionalScopes: null);
        var differentEmail = Faker.Internet.Email();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/resend-email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", differentEmail }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/sign-in/email?{authStateHelper.ToQueryParam()}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_EmailAlreadyVerified_RedirectsToNextPage()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified(), additionalScopes: null);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/resend-email-confirmation?{authStateHelper.ToQueryParam()}")
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
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_EmptyEmail_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet(), additionalScopes: null);
        var differentEmail = "";

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/resend-email-confirmation?{authStateHelper.ToQueryParam()}")
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
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet(), additionalScopes: null);
        var differentEmail = "xx";

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/resend-email-confirmation?{authStateHelper.ToQueryParam()}")
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
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet(), additionalScopes: null);
        var differentEmail = "valid@email.com";

        HostFixture.RateLimitStore.Setup(x => x.IsClientIpBlockedForPinGeneration(TestRequestClientIpProvider.ClientIpAddress)).ReturnsAsync(true);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/resend-email-confirmation?{authStateHelper.ToQueryParam()}")
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
    public async Task Post_ValidRequest_SetsEmailOnAuthenticationStateGeneratesPinAndRedirectsToConfirmation()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet(), additionalScopes: null);
        var differentEmail = Faker.Internet.Email();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/resend-email-confirmation?{authStateHelper.ToQueryParam()}")
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
        Assert.Equal($"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}", response.Headers.Location?.OriginalString);

        Assert.Equal(differentEmail, authStateHelper.AuthenticationState.EmailAddress);

        HostFixture.EmailVerificationService.Verify(mock => mock.GeneratePin(differentEmail), Times.Once);
    }

    [Theory]
    [InlineData("admin")]
    [InlineData("academy")]
    public async Task Post_EmailWithInvalidPrefix_ReturnsError(string emailPrefix)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet(), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/resend-email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", TestData.GenerateUniqueEmail(emailPrefix) }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Email", "Enter a personal email address. It cannot be one that other people may get access to.");
    }

    [Fact]
    public async Task Post_EmailWithInvalidPrefixAlreadyExists_DoNotReturnError()
    {
        // Arrange
        var invalidPrefix = "headteacher";
        var user = await TestData.CreateUser(email: TestData.GenerateUniqueEmail(invalidPrefix));

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet(), additionalScopes: null);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/resend-email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", user.EmailAddress }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_NotificationServiceInvalidEmail_ReturnsError()
    {
        // Arrange
        HostFixture.EmailSender
            .Setup(mock => mock.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new Exception("ValidationError"));

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet(), additionalScopes: null);
        var email = Faker.Internet.Email();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/resend-email-confirmation?{authStateHelper.ToQueryParam()}")
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
}
