using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Register;

public class HasTrnPageTests : TestBase
{
    public HasTrnPageTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/register/has-trn");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/register/has-trn");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.DqtRead, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/has-trn");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(), CustomScopes.DqtRead, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/has-trn");
    }

    [Fact]
    public async Task Get_RequiresTrnLookupFalse_ReturnsBadRequest()
    {
        await JourneyRequiresTrnLookup_TrnLookupRequiredIsFalse_ReturnsBadRequest(
            _currentPageAuthenticationState(),
            HttpMethod.Get,
            "/sign-in/register/has-trn");
    }

    [Fact]
    public async Task Get_HasNinoNotSet_RedirectsToRegisterHasNiNumber()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_hasNinoPageAuthenticationState(), CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/register/has-trn?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/register/has-nino", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_NinoNotSet_RedirectsToRegisterNiNumber()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_ninoPageAuthenticationState(), CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/register/has-trn?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/register/ni-number", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersContent()
    {
        await ValidRequest_RendersContent(_currentPageAuthenticationState(), CustomScopes.DqtRead, trnRequirementType: null, url: "/sign-in/register/has-trn");
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/register/has-trn");
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/register/has-trn");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.DqtRead, trnRequirementType: null, HttpMethod.Post, "/sign-in/register/has-trn");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(), CustomScopes.DqtRead, trnRequirementType: null, HttpMethod.Post, "/sign-in/register/has-trn");
    }

    [Fact]
    public async Task Post_NullHasTrn_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), CustomScopes.DqtRead);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/has-trn?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "HasTrn", "Tell us if you know your TRN");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Post_ValidHasTrn_UpdatesAuthenticationStateRedirectsToTrn(bool hasTrn)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/has-trn?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasTrn", hasTrn },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);

        if (hasTrn)
        {
            Assert.StartsWith("/sign-in/register/trn", response.Headers.Location?.OriginalString);
        }
        else
        {
            Assert.StartsWith("/sign-in/register/has-qts", response.Headers.Location?.OriginalString);
        }

        Assert.Equal(hasTrn, authStateHelper.AuthenticationState.HasTrn);
    }

    [Fact]
    public async Task Post_FalseRequiresTrnLookup_ReturnsBadRequest()
    {
        var content = new FormUrlEncodedContentBuilder()
        {
            { "HasTrn", true },
        };

        await JourneyRequiresTrnLookup_TrnLookupRequiredIsFalse_ReturnsBadRequest(
            _currentPageAuthenticationState(),
            HttpMethod.Post,
            "/sign-in/register/has-trn",
            content);
    }

    [Fact]
    public async Task Post_TrnLookupFindsExactlyOneResultAndHasTrnFalse_RedirectsToCheckAnswersPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), CustomScopes.DqtRead);
        ConfigureDqtApiClientToReturnSingleMatch(authStateHelper);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/has-trn?{authStateHelper.ToQueryParam()}")
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
        Assert.StartsWith("/sign-in/register/check-answers", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_HasTrnTrue_DoesNotAttemptTrnLookup()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), CustomScopes.DqtRead);
        ConfigureDqtApiClientToReturnSingleMatch(authStateHelper);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/has-trn?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasTrn", true },
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
    public async Task Post_HasTrnAllQuestionsAnswered_RedirectsToCorrectPage(bool hasTrn)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_allQuestionsAnsweredAuthenticationState(), CustomScopes.DqtRead);
        ConfigureDqtApiClientToReturnSingleMatch(authStateHelper);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/has-trn?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasTrn", hasTrn },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith(hasTrn ? "/sign-in/register/trn" : "/sign-in/register/check-answers", response.Headers.Location?.OriginalString);
    }

    private readonly AuthenticationStateConfigGenerator _currentPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.HasTrn);
    private readonly AuthenticationStateConfigGenerator _ninoPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.NiNumber);
    private readonly AuthenticationStateConfigGenerator _hasNinoPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.HasNiNumber);
    private readonly AuthenticationStateConfigGenerator _allQuestionsAnsweredAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.CheckAnswers);
}
