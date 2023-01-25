namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Trn;

[Collection(nameof(DisableParallelization))]  // Relies on mocks
public class HaveNiNumberTests : TestBase
{
    public HaveNiNumberTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn/have-nino");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn/have-nino");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(HttpMethod.Get, "/sign-in/trn/have-nino");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(c => c.OfficialNameSet(), HttpMethod.Get, "/sign-in/trn/have-nino");
    }

    [Fact]
    public async Task Get_NoEmail_RedirectsToEmailPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Start());

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/have-nino?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/email", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_EmailNotVerified_RedirectsToEmailConfirmationPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet());

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/have-nino?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/email-confirmation", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_OfficialNameNotSet_RedirectsToNextHopUrl()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified());

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/have-nino?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersContent()
    {
        await ValidRequest_RendersContent("/sign-in/trn/have-nino", c => c.OfficialNameSet());
    }

    [Fact]
    public async Task Post_NullHasNiNumber_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.OfficialNameSet());
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/have-nino?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "HasNiNumber", "Tell us if you have a National Insurance number");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Post_ValidForm_SetsHaveNiNumberOnAuthenticationState(bool hasNiNumber)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.OfficialNameSet());

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/have-nino?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasNiNumber", hasNiNumber },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(hasNiNumber, authStateHelper.AuthenticationState.HaveNationalInsuranceNumber);
    }

    [Fact]
    public async Task Post_HasNiNumberTrue_RedirectsToNiNumberPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.OfficialNameSet());

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/have-nino?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasNiNumber", true },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/ni-number", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_HasNiNumberFalse_RedirectsToAwardedQtsPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.OfficialNameSet());

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/have-nino?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasNiNumber", false },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/awarded-qts", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_TrnLookupFindsExactlyOneResultAndHasNiNumberFalse_RedirectsToCheckAnswersPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.OfficialNameSet());
        ConfigureDqtApiClientToReturnSingleMatch(authStateHelper);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/have-nino?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasNiNumber", false },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/check-answers", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_NiNumberTrue_DoesNotAttemptTrnLookup()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.OfficialNameSet());
        ConfigureDqtApiClientToReturnSingleMatch(authStateHelper);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/have-nino?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasNiNumber", true },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        VerifyDqtApiFindTeachersNotCalled();
    }
}
