using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Register;

public class HasQtsPageTests : TestBase
{
    public HasQtsPageTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/register/has-qts");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/register/has-qts");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.DqtRead, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/has-qts");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(), CustomScopes.DqtRead, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/has-qts");
    }

    [Fact]
    public async Task Get_RequiresTrnLookupFalse_ReturnsBadRequest()
    {
        await JourneyRequiresTrnLookup_TrnLookupRequiredIsFalse_ReturnsBadRequest(
            _currentPageAuthenticationState(),
            HttpMethod.Get,
            "/sign-in/register/has-qts");
    }

    [Fact]
    public async Task Get_HasTrnNotSet_RedirectsToHasTrnPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_hasTrnPageAuthenticationState(), CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/register/has-qts?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/register/has-trn", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_HasTrnButTrnNotSet_RedirectsToTrnPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_trnPageAuthenticationState(), CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/register/has-qts?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/register/trn", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersContent()
    {
        await ValidRequest_RendersContent(_currentPageAuthenticationState(), CustomScopes.DqtRead, trnRequirementType: null, url: "/sign-in/register/has-qts");
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/register/has-qts");
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/register/has-qts");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.DqtRead, trnRequirementType: null, HttpMethod.Post, "/sign-in/register/has-qts");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(), CustomScopes.DqtRead, trnRequirementType: null, HttpMethod.Post, "/sign-in/register/has-qts");
    }

    [Fact]
    public async Task Post_HasTrnNotSet_RedirectsToHasTrnPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_hasTrnPageAuthenticationState(), CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/has-qts?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/register/has-trn", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_RequiresTrnLookupFalse_ReturnsBadRequest()
    {
        var content = new FormUrlEncodedContentBuilder()
        {
            { "AwardedQts", true },
        };

        await JourneyRequiresTrnLookup_TrnLookupRequiredIsFalse_ReturnsBadRequest(
            _currentPageAuthenticationState(),
            HttpMethod.Post,
            "/sign-in/register/has-qts",
            content);
    }

    [Fact]
    public async Task Post_HasTrnButTrnNotSet_RedirectsToTrnPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_trnPageAuthenticationState(), CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/has-qts?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/register/trn", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_NullAwardedQts_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), CustomScopes.DqtRead);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/has-qts?{authStateHelper.ToQueryParam()}")
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
    public async Task Post_ValidForm_SetsAwardedQtsOnAuthenticationState(bool awardedQts)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/has-qts?{authStateHelper.ToQueryParam()}")
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
    }

    [Fact]
    public async Task Post_TrnLookupFindsExactlyOneResultAndHasQtsFalse_RedirectsToCheckAnswersPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), CustomScopes.DqtRead);
        ConfigureDqtApiClientToReturnSingleMatch(authStateHelper);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/has-qts?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "AwardedQts", false },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/register/check-answers", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_HasQtsTrue_DoesNotAttemptTrnLookup()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), CustomScopes.DqtRead);
        ConfigureDqtApiClientToReturnSingleMatch(authStateHelper);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/has-qts?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "AwardedQts", true },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        VerifyDqtApiFindTeachersNotCalled();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Post_HasQtsAllQuestionsAnswered_RedirectsToCorrectPage(bool awardedQts)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_allQuestionsAnsweredAuthenticationState(), CustomScopes.DqtRead);
        ConfigureDqtApiClientToReturnSingleMatch(authStateHelper);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/has-qts?{authStateHelper.ToQueryParam()}")
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
        Assert.StartsWith(awardedQts ? "/sign-in/register/itt-provider" : "/sign-in/register/check-answers", response.Headers.Location?.OriginalString);
    }

    private readonly AuthenticationStateConfigGenerator _currentPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.HasQts);
    private readonly AuthenticationStateConfigGenerator _trnPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.Trn);
    private readonly AuthenticationStateConfigGenerator _hasTrnPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.HasTrn);
    private readonly AuthenticationStateConfigGenerator _allQuestionsAnsweredAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.CheckAnswers);
}
