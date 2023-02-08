namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Trn;

[Collection(nameof(DisableParallelization))]  // Relies on mocks
public class HasTrnPageTests : TestBase
{
    public HasTrnPageTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn/has-trn");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn/has-trn");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(HttpMethod.Get, "/sign-in/trn/has-trn");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(ConfigureValidAuthenticationState, HttpMethod.Get, "/sign-in/trn/has-trn");
    }

    [Theory]
    [IncompleteAuthenticationMilestonesData(AuthenticationState.AuthenticationMilestone.EmailVerified)]
    public async Task Get_JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(
        AuthenticationState.AuthenticationMilestone milestone)
    {
        await JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(milestone, HttpMethod.Get, "/sign-in/trn/has-trn");
    }

    [Fact]
    public async Task Get_ValidRequest_RendersContent()
    {
        await ValidRequest_RendersContent("/sign-in/trn/has-trn", ConfigureValidAuthenticationState);
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/trn/has-trn");
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/trn/has-trn");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(HttpMethod.Post, "/sign-in/trn/has-trn");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(ConfigureValidAuthenticationState, HttpMethod.Post, "/sign-in/trn/has-trn");
    }

    [Theory]
    [IncompleteAuthenticationMilestonesData(AuthenticationState.AuthenticationMilestone.EmailVerified)]
    public async Task Post_JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(
        AuthenticationState.AuthenticationMilestone milestone)
    {
        await JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(milestone, HttpMethod.Post, "/sign-in/trn/has-trn");
    }

    [Fact]
    public async Task Post_NullHasTrn_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/has-trn?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "HasTrn", "Tell us if you know your TRN");
    }

    [Fact]
    public async Task Post_EmptyStatedTrn_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/has-trn?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasTrn", true },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "StatedTrn", "Enter your TRN");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("123456")]
    [InlineData("1234567 8")]
    public async Task Post_InvalidStatedTrn_ReturnsError(string statedTrn)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/has-trn?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasTrn", true },
                { "StatedTrn", statedTrn },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "StatedTrn", "Your TRN number should contain 7 digits");
    }

    [Fact]
    public async Task Post_FalseHasTrn_UpdatesAuthenticationStateRedirectsToTrnOfficialName()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/has-trn?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasTrn", false },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/official-name", response.Headers.Location?.OriginalString);

        Assert.False(authStateHelper.AuthenticationState.HasTrn);
    }

    [Theory]
    [InlineData("4567814")]
    [InlineData("RP99/12345")]
    public async Task Post_ValidStatedTrn_UpdatesAuthenticationStateRedirectsToTrnOfficialName(string statedTrn)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/has-trn?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasTrn", true },
                { "StatedTrn", statedTrn },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/official-name", response.Headers.Location?.OriginalString);

        Assert.True(authStateHelper.AuthenticationState.HasTrn);
        Assert.Equal(statedTrn, authStateHelper.AuthenticationState.StatedTrn);
    }

    [Fact]
    public async Task Post_TrnLookupFindsExactlyOneResult_RedirectsToCheckAnswersPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState);
        ConfigureDqtApiClientToReturnSingleMatch(authStateHelper);

        var statedTrn = TestData.GenerateTrn();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/has-trn?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasTrn", true },
                { "StatedTrn", statedTrn },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/check-answers", response.Headers.Location?.OriginalString);
    }

    private Func<AuthenticationState, Task> ConfigureValidAuthenticationState(AuthenticationStateHelper.Configure configure) =>
        configure.EmailVerified();
}
