using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Register;

public class TrnPageTests : TestBase
{
    public TrnPageTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/register/trn");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/register/trn");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.DqtRead, HttpMethod.Get, "/sign-in/register/trn");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(), CustomScopes.DqtRead, HttpMethod.Get, "/sign-in/register/trn");
    }

    [Fact]
    public async Task Get_HasTrnNotSet_RedirectsToHasTrn()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_previousPageAuthenticationState(), CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/register/trn?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/register/has-trn", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_RequiresTrnLookupFalse_ReturnsBadRequest()
    {
        await JourneyRequiresTrnLookup_TrnLookupRequiredIsFalse_ReturnsBadRequest(
            _currentPageAuthenticationState(),
            HttpMethod.Get,
            "/sign-in/register/trn");
    }

    [Fact]
    public async Task Get_ValidRequest_RendersContent()
    {
        await ValidRequest_RendersContent(_currentPageAuthenticationState(), "/sign-in/register/trn", CustomScopes.DqtRead);
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/register/trn");
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/register/trn");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.DqtRead, HttpMethod.Post, "/sign-in/register/trn");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(), CustomScopes.DqtRead, HttpMethod.Post, "/sign-in/register/trn");
    }

    [Fact]
    public async Task Post_EmptyStatedTrn_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), CustomScopes.DqtRead);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/trn?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "submit", "submit" },
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
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/trn?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "StatedTrn", statedTrn },
                { "submit", "submit" },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "StatedTrn", "Your TRN number should contain 7 digits");
    }

    [Fact]
    public async Task Post_FalseRequiresTrnLookup_ReturnsBadRequest()
    {
        var content = new FormUrlEncodedContentBuilder()
        {
            { "StatedTrn", TestData.GenerateTrn() },
            { "submit", "submit" },
        };

        await JourneyRequiresTrnLookup_TrnLookupRequiredIsFalse_ReturnsBadRequest(
            _currentPageAuthenticationState(),
            HttpMethod.Post,
            "/sign-in/register/trn",
            content);
    }

    [Fact]
    public async Task Post_HasTrnNotSet_RedirectsToHasTrn()
    {
        // Arrange
        var statedTrn = TestData.GenerateTrn();
        var authStateHelper = await CreateAuthenticationStateHelper(_previousPageAuthenticationState(), CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/trn?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "StatedTrn", statedTrn },
                { "submit", "submit" },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/register/has-trn", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_ValidStatedTrn_UpdatesAuthenticationStateAndRedirects()
    {
        // Arrange
        var statedTrn = TestData.GenerateTrn();
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/trn?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "StatedTrn", statedTrn },
                { "submit", "submit" },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/register/has-qts", response.Headers.Location?.OriginalString);

        Assert.Equal(statedTrn, authStateHelper.AuthenticationState.StatedTrn);
    }

    [Fact]
    public async Task Post_ValidStatedTrnAllQuestionsAnswered_RedirectsToCheckAnswers()
    {
        // Arrange
        var statedTrn = TestData.GenerateTrn();
        var authStateHelper = await CreateAuthenticationStateHelper(_allQuestionsAnsweredAuthenticationState(), CustomScopes.DqtRead);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/trn?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "StatedTrn", statedTrn },
                { "submit", "submit" },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/register/check-answers", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_TrnLookupFindsExactlyOneResult_RedirectsToCheckAnswersPage()
    {
        // Arrange
        var statedTrn = TestData.GenerateTrn();
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), CustomScopes.DqtRead);
        ConfigureDqtApiClientToReturnSingleMatch(authStateHelper);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/trn?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "StatedTrn", statedTrn },
                { "submit", "submit" },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/register/check-answers", response.Headers.Location?.OriginalString);
    }

    private readonly AuthenticationStateConfigGenerator _currentPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.Trn);
    private readonly AuthenticationStateConfigGenerator _previousPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.HasTrn);
    private readonly AuthenticationStateConfigGenerator _allQuestionsAnsweredAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.CheckAnswers);
}
