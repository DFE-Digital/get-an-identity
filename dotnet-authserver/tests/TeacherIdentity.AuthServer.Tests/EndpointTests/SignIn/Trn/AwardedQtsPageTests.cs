namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Trn;

[Collection(nameof(DisableParallelization))]  // Relies on mocks
public class AwardedQtsPageTests : TestBase
{
    public AwardedQtsPageTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn/awarded-qts");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn/awarded-qts");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(HttpMethod.Get, "/sign-in/trn/awarded-qts");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(c => c.OfficialNameSet(), HttpMethod.Get, "/sign-in/trn/awarded-qts");
    }

    [Fact]
    public async Task Get_NoEmail_RedirectsToEmailPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Start());

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/awarded-qts?{authStateHelper.ToQueryParam()}");

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

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/awarded-qts?{authStateHelper.ToQueryParam()}");

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

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/awarded-qts?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersContent()
    {
        await ValidRequest_RendersContent("/sign-in/trn/awarded-qts", c => c.OfficialNameSet());
    }

    [Fact]
    public async Task Post_NullAwardedQts_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.OfficialNameSet());
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/awarded-qts?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "AwardedQts", "Tell us if you have been awarded qualified teacher status (QTS)");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Post_ValidForm_SetsAwardedQtsOnAuthenticationStateRedirectsToCorrectPage(bool awardedQts)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.OfficialNameSet());

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/awarded-qts?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "AwardedQts", awardedQts },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(awardedQts, authStateHelper.AuthenticationState.AwardedQts);

        if (awardedQts)
        {
            Assert.StartsWith("/sign-in/trn/itt-provider", response.Headers.Location?.OriginalString);
        }
        else
        {
            Assert.StartsWith("/sign-in/trn/check-answers", response.Headers.Location?.OriginalString);
        }
    }

    [Fact]
    public async Task Post_TrnLookupFindsExactlyOneResultAndAwardedQtsFalse_RedirectsToCheckAnswersPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.OfficialNameSet());
        ConfigureDqtApiClientToReturnSingleMatch(authStateHelper);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/awarded-qts?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "AwardedQts", bool.FalseString },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/check-answers", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_AwardedQtsTrue_DoesNotAttemptTrnLookup()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.OfficialNameSet());
        ConfigureDqtApiClientToReturnSingleMatch(authStateHelper);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/awarded-qts?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "AwardedQts", bool.TrueString },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        VerifyDqtApiFindTeachersNotCalled();
    }
}
