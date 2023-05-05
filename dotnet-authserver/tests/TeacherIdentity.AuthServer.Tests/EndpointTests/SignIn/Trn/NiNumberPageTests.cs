using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Trn;

public class NiNumberPageTests : TestBase
{
    public NiNumberPageTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/trn/ni-number");
    }

    [Fact]
    public async Task Get_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/trn/ni-number");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.Trn, TrnRequirementType.Legacy, HttpMethod.Get, "/sign-in/trn/ni-number");
    }

    [Fact]
    public async Task Get_UserRequirementsDoesNotContainTrnHolder_ReturnsForbidden()
    {
        await InvalidUserRequirements_ReturnsForbidden(ConfigureValidAuthenticationState, additionalScopes: null, TrnRequirementType.Legacy, HttpMethod.Get, "/sign-in/trn/ni-number");
    }

    [Theory]
    [IncompleteAuthenticationMilestonesData(AuthenticationState.AuthenticationMilestone.EmailVerified)]
    public async Task Get_JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(
        AuthenticationState.AuthenticationMilestone milestone)
    {
        await JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(milestone, CustomScopes.Trn, TrnRequirementType.Legacy, HttpMethod.Get, "/sign-in/trn/ni-number");
    }

    [Fact]
    public async Task Get_HaveNationalInsuranceNumberNotSet_RedirectsToHasNiNumberPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Trn.DateOfBirthSet(), CustomScopes.Trn, TrnRequirementType.Legacy);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/ni-number?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/has-nino", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersContent()
    {
        await ValidRequest_RendersContent(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy, "/sign-in/trn/ni-number");
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/trn/ni-number");
    }

    [Fact]
    public async Task Post_MissingAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/trn/ni-number");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.Trn, TrnRequirementType.Legacy, HttpMethod.Post, "/sign-in/trn/ni-number");
    }

    [Fact]
    public async Task Post_UserRequirementsDoesNotContainTrnHolder_ReturnsForbidden()
    {
        await InvalidUserRequirements_ReturnsForbidden(ConfigureValidAuthenticationState, additionalScopes: null, TrnRequirementType.Legacy, HttpMethod.Post, "/sign-in/trn/ni-number");
    }

    [Theory]
    [IncompleteAuthenticationMilestonesData(AuthenticationState.AuthenticationMilestone.EmailVerified)]
    public async Task Post_JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(
        AuthenticationState.AuthenticationMilestone milestone)
    {
        await JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(milestone, CustomScopes.Trn, TrnRequirementType.Legacy, HttpMethod.Post, "/sign-in/trn/ni-number");
    }

    [Fact]
    public async Task Post_HaveNationalInsuranceNumberNotSet_RedirectsToHasNiNumberPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Trn.DateOfBirthSet(), CustomScopes.Trn, TrnRequirementType.Legacy);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/ni-number?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/has-nino", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_EmptyNiNumber_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/ni-number?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "NiNumber", "Enter a National Insurance number");
    }

    [Theory]
    [InlineData("x")]
    [InlineData("zyx")]
    public async Task Post_InvalidNiNumber_ReturnsError(string niNumber)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/ni-number?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "NiNumber", niNumber },
                { "submit", "submit" },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "NiNumber", "Enter a National Insurance number in the correct format");
    }

    [Theory]
    [InlineData("AB 12 34 56 C")]
    [InlineData("AB123456C")]
    [InlineData("aB123456c")]
    public async Task Post_ValidNiNumber_SetsNiNumberOnAuthenticationStateRedirectsToAwardedQtsPage(string niNumber)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/ni-number?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "NiNumber", niNumber },
                { "submit", "submit" },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/awarded-qts", response.Headers.Location?.OriginalString);

        Assert.Equal(niNumber, authStateHelper.AuthenticationState.NationalInsuranceNumber);
    }

    [Fact]
    public async Task Post_NiNumberNotKnown_SetsHasNiNumberFalseAndRedirectsToAwardedQtsPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/ni-number?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "submit", "ni_number_not_known" },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/awarded-qts", response.Headers.Location?.OriginalString);

        Assert.False(authStateHelper.AuthenticationState.HasNationalInsuranceNumber);
    }

    [Fact]
    public async Task Post_TrnLookupFindsExactlyOneResult_RedirectsToCheckAnswersPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy);
        ConfigureDqtApiClientToReturnSingleMatch(authStateHelper);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/ni-number?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "NiNumber", "AB123456C" },
                { "submit", "submit" },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/check-answers", response.Headers.Location?.OriginalString);
    }

    private Func<AuthenticationState, Task> ConfigureValidAuthenticationState(AuthenticationStateHelper.Configure configure) =>
        configure.Trn.HasNationalInsuranceNumberSet(hasNationalInsuranceNumber: true);
}
