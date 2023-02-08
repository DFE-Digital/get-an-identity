namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Trn;

[Collection(nameof(DisableParallelization))]  // Relies on mocks
public class DateOfBirthPageTests : TestBase
{
    public DateOfBirthPageTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn/date-of-birth");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn/date-of-birth");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(HttpMethod.Get, "/sign-in/trn/date-of-birth");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(ConfigureValidAuthenticationState, HttpMethod.Get, "/sign-in/trn/date-of-birth");
    }

    [Theory]
    [IncompleteAuthenticationMilestonesData(AuthenticationState.AuthenticationMilestone.EmailVerified)]
    public async Task Get_JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(
        AuthenticationState.AuthenticationMilestone milestone)
    {
        await JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(milestone, HttpMethod.Get, "/sign-in/trn/date-of-birth");
    }

    [Fact]
    public async Task Get_PreferredNameNotSet_RedirectsToPreferredNamePage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Trn.OfficialNameSet());

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/date-of-birth?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/preferred-name", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersContent()
    {
        await ValidRequest_RendersContent("/sign-in/trn/date-of-birth", ConfigureValidAuthenticationState);
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/trn/date-of-birth");
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/trn/date-of-birth");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(HttpMethod.Post, "/sign-in/trn/date-of-birth");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(ConfigureValidAuthenticationState, HttpMethod.Post, "/sign-in/trn/date-of-birth");
    }

    [Theory]
    [IncompleteAuthenticationMilestonesData(AuthenticationState.AuthenticationMilestone.EmailVerified)]
    public async Task Post_JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(
        AuthenticationState.AuthenticationMilestone milestone)
    {
        await JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(milestone, HttpMethod.Post, "/sign-in/trn/date-of-birth");
    }

    [Fact]
    public async Task Post_PreferredNameNotSet_RedirectsToPreferredNamePage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Trn.OfficialNameSet());

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/date-of-birth?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/preferred-name", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_NullDateOfBirth_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/date-of-birth?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "DateOfBirth", "Enter your date of birth");
    }

    [Fact]
    public async Task Post_FutureDateOfBirth_ReturnsError()
    {
        // Arrange
        var dateOfBirth = new DateOnly(2100, 1, 1);

        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/date-of-birth?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "DateOfBirth.Day", dateOfBirth.Day.ToString() },
                { "DateOfBirth.Month", dateOfBirth.Month.ToString() },
                { "DateOfBirth.Year", dateOfBirth.Year.ToString() },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "DateOfBirth", "Your date of birth must be in the past");
    }

    [Fact]
    public async Task Post_ValidForm_SetsDateOfBirthOnAuthenticationStateRedirectsToTrnHasNiNumberPage()
    {
        // Arrange
        var dateOfBirth = new DateOnly(2000, 1, 1);

        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/date-of-birth?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "DateOfBirth.Day", dateOfBirth.Day.ToString() },
                { "DateOfBirth.Month", dateOfBirth.Month.ToString() },
                { "DateOfBirth.Year", dateOfBirth.Year.ToString() },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/has-nino", response.Headers.Location?.OriginalString);

        Assert.Equal(dateOfBirth, authStateHelper.AuthenticationState.DateOfBirth);
    }

    [Fact]
    public async Task Post_TrnLookupFindsExactlyOneResult_RedirectsToCheckAnswersPage()
    {
        // Arrange
        var dateOfBirth = new DateOnly(2000, 1, 1);
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState);

        ConfigureDqtApiClientToReturnSingleMatch(authStateHelper);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/date-of-birth?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "DateOfBirth.Day", dateOfBirth.Day.ToString() },
                { "DateOfBirth.Month", dateOfBirth.Month.ToString() },
                { "DateOfBirth.Year", dateOfBirth.Year.ToString() },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/check-answers", response.Headers.Location?.OriginalString);
    }

    private Func<AuthenticationState, Task> ConfigureValidAuthenticationState(AuthenticationStateHelper.Configure configure) =>
        configure.Trn.PreferredNameSet();
}
