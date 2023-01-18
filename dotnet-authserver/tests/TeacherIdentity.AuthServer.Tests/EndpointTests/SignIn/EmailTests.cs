using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

[Collection(nameof(DisableParallelization))]  // Depends on mocks
public class EmailTests : TestBase
{
    public EmailTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/email");
    }

    [Fact]
    public async Task Get_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/email");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(HttpMethod.Get, "/sign-in/email");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(c => c.Start(), HttpMethod.Get, "/sign-in/email");
    }

    [Fact]
    public async Task Get_ValidRequest_RendersContent()
    {
        await ValidRequest_RendersContent("/sign-in/email", c => c.Start());
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/email");
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/email");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(HttpMethod.Post, "/sign-in/email");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(c => c.Start(), HttpMethod.Post, "/sign-in/email");
    }

    [Fact]
    public async Task Post_ValidEmailWithBlockedClient_ReturnsTooManyRequestsStatusCode()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Start());
        var email = Faker.Internet.Email();

        HostFixture.RateLimitStore.Setup(x => x.IsClientIpBlockedForPinGeneration(TestRequestClientIpProvider.ClientIpAddress)).ReturnsAsync(true);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", email }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status429TooManyRequests, (int)response.StatusCode);
    }


    [Fact]
    public async Task Post_EmptyEmail_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Start());
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
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
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Start());
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", "xxx" }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Email", "Enter a valid email address");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Post_ValidEmail_SetsEmailOnAuthenticationStateGeneratesPinAndRedirectsToConfirmation(bool emailIsKnown)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Start());
        var email = Faker.Internet.Email();

        if (emailIsKnown)
        {
            await TestData.CreateUser(email);
        }

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email?{authStateHelper.ToQueryParam()}")
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
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);

        Assert.Equal(email, authStateHelper.AuthenticationState.EmailAddress);

        HostFixture.EmailVerificationService.Verify(mock => mock.GeneratePin(email), Times.Once);
    }
}
