using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Trn;

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
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.Trn, TrnRequirementType.Legacy, HttpMethod.Get, "/sign-in/trn/date-of-birth");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy, HttpMethod.Get, "/sign-in/trn/date-of-birth");
    }

    [Fact]
    public async Task Get_UserRequirementsDoesNotContainTrnHolder_ReturnsForbidden()
    {
        await InvalidUserRequirements_ReturnsForbidden(ConfigureValidAuthenticationState, additionalScopes: null, TrnRequirementType.Legacy, HttpMethod.Get, "/sign-in/trn/date-of-birth");
    }

    [Theory]
    [IncompleteAuthenticationMilestonesData(AuthenticationState.AuthenticationMilestone.EmailVerified)]
    public async Task Get_JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(
        AuthenticationState.AuthenticationMilestone milestone)
    {
        await JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(milestone, CustomScopes.Trn, TrnRequirementType.Legacy, HttpMethod.Get, "/sign-in/trn/date-of-birth");
    }

    [Fact]
    public async Task Get_PreferredNameNotSet_RedirectsToPreferredNamePage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Trn.OfficialNameSet(), CustomScopes.Trn, TrnRequirementType.Legacy);

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
        await ValidRequest_RendersContent(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy, "/sign-in/trn/date-of-birth");
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
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.Trn, TrnRequirementType.Legacy, HttpMethod.Post, "/sign-in/trn/date-of-birth");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy, HttpMethod.Post, "/sign-in/trn/date-of-birth");
    }

    [Fact]
    public async Task Post_UserRequirementsDoesNotContainTrnHolder_ReturnsForbidden()
    {
        await InvalidUserRequirements_ReturnsForbidden(ConfigureValidAuthenticationState, additionalScopes: null, TrnRequirementType.Legacy, HttpMethod.Post, "/sign-in/trn/date-of-birth");
    }

    [Theory]
    [IncompleteAuthenticationMilestonesData(AuthenticationState.AuthenticationMilestone.EmailVerified)]
    public async Task Post_JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(
        AuthenticationState.AuthenticationMilestone milestone)
    {
        await JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(milestone, CustomScopes.Trn, TrnRequirementType.Legacy, HttpMethod.Post, "/sign-in/trn/date-of-birth");
    }

    [Fact]
    public async Task Post_PreferredNameNotSet_RedirectsToPreferredNamePage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Trn.OfficialNameSet(), CustomScopes.Trn, TrnRequirementType.Legacy);

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
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy);
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

        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy);
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

        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy);
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
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy);

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
