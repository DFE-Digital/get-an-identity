using TeacherIdentity.AuthServer.Oidc;

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
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.DqtRead, HttpMethod.Get, "/sign-in/trn/awarded-qts");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(ConfigureValidAuthenticationState, CustomScopes.DqtRead, HttpMethod.Get, "/sign-in/trn/awarded-qts");
    }

    [Theory]
    [IncompleteAuthenticationMilestonesData(AuthenticationState.AuthenticationMilestone.EmailVerified)]
    public async Task Get_JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(
        AuthenticationState.AuthenticationMilestone milestone)
    {
        await JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(milestone, HttpMethod.Get, "/sign-in/trn/awarded-qts");
    }

    [Fact]
    public async Task Get_HasNinoNotSet_RedirectsToHasNinoPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Trn.DateOfBirthSet(), CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/awarded-qts?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/has-nino", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_HasNinoButNinoNotSet_RedirectsToHasNiNumberPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.Trn.HasNationalInsuranceNumberSet(hasNationalInsuranceNumber: true),
            CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/awarded-qts?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/ni-number", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersContent()
    {
        await ValidRequest_RendersContent("/sign-in/trn/awarded-qts", ConfigureValidAuthenticationState, CustomScopes.DqtRead);
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/trn/awarded-qts");
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/trn/awarded-qts");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.DqtRead, HttpMethod.Post, "/sign-in/trn/awarded-qts");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(ConfigureValidAuthenticationState, CustomScopes.DqtRead, HttpMethod.Post, "/sign-in/trn/awarded-qts");
    }

    [Theory]
    [IncompleteAuthenticationMilestonesData(AuthenticationState.AuthenticationMilestone.EmailVerified)]
    public async Task Post_JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(
        AuthenticationState.AuthenticationMilestone milestone)
    {
        await JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(milestone, HttpMethod.Post, "/sign-in/trn/awarded-qts");
    }

    [Fact]
    public async Task Post_HasNinoNotSet_RedirectsToHasNinoPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Trn.DateOfBirthSet(), CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/awarded-qts?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/has-nino", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_HasNinoButNinoNotSet_RedirectsToHasNiNumberPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(
            c => c.Trn.HasNationalInsuranceNumberSet(hasNationalInsuranceNumber: true),
            CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/awarded-qts?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/ni-number", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_NullAwardedQts_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.DqtRead);
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
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.DqtRead);

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
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.DqtRead);
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
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.DqtRead);
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

    private Func<AuthenticationState, Task> ConfigureValidAuthenticationState(AuthenticationStateHelper.Configure configure) =>
        configure.Trn.NationalInsuranceNumberSet();
}
