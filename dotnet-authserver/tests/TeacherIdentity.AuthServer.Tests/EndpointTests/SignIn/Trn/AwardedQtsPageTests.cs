using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Trn;

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
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.Trn, TrnRequirementType.Legacy, HttpMethod.Get, "/sign-in/trn/awarded-qts");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy, HttpMethod.Get, "/sign-in/trn/awarded-qts");
    }

    [Fact]
    public async Task Get_UserRequirementsDoesNotContainTrnHolder_ReturnsBadRequest()
    {
        await InvalidUserRequirements_ReturnsBadRequest(ConfigureValidAuthenticationState, additionalScopes: null, TrnRequirementType.Legacy, HttpMethod.Get, "/sign-in/trn/awarded-qts");
    }

    [Fact]
    public async Task Get_HasNinoNotSet_RedirectsToHasNinoPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Trn.DateOfBirthSet(), CustomScopes.Trn, TrnRequirementType.Legacy);

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
            CustomScopes.Trn,
            TrnRequirementType.Legacy);

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
        await ValidRequest_RendersContent(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy, "/sign-in/trn/awarded-qts");
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
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.Trn, TrnRequirementType.Legacy, HttpMethod.Post, "/sign-in/trn/awarded-qts");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy, HttpMethod.Post, "/sign-in/trn/awarded-qts");
    }

    [Fact]
    public async Task Post_UserRequirementsDoesNotContainTrnHolder_ReturnsBadRequest()
    {
        await InvalidUserRequirements_ReturnsBadRequest(ConfigureValidAuthenticationState, additionalScopes: null, TrnRequirementType.Legacy, HttpMethod.Post, "/sign-in/trn/awarded-qts");
    }

    [Fact]
    public async Task Post_HasNinoNotSet_RedirectsToHasNinoPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Trn.DateOfBirthSet(), CustomScopes.Trn, TrnRequirementType.Legacy);

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
            CustomScopes.Trn,
            TrnRequirementType.Legacy);

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
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy);
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
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy);

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
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy);
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
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy);
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
