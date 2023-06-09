using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Trn;

public class OfficialNameTests : TestBase
{
    public OfficialNameTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn/official-name");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn/official-name");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.Trn, TrnRequirementType.Legacy, HttpMethod.Get, "/sign-in/trn/official-name");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy, HttpMethod.Get, "/sign-in/trn/official-name");
    }

    [Fact]
    public async Task Get_UserRequirementsDoesNotContainTrnHolder_ReturnsBadRequest()
    {
        await InvalidUserRequirements_ReturnsBadRequest(ConfigureValidAuthenticationState, additionalScopes: null, trnRequirementType: null, HttpMethod.Get, "/sign-in/trn/official-name");
    }

    [Fact]
    public async Task Get_HasTrnNotSet_RedirectsToHasTrnPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified(), CustomScopes.Trn, TrnRequirementType.Legacy);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/official-name?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/has-trn", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersContent()
    {
        await ValidRequest_RendersContent(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy, "/sign-in/trn/official-name");
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/trn/official-name");
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/trn/official-name");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(CustomScopes.Trn, TrnRequirementType.Legacy, HttpMethod.Post, "/sign-in/trn/official-name");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy, HttpMethod.Post, "/sign-in/trn/official-name");
    }

    [Fact]
    public async Task Post_UserRequirementsDoesNotContainTrnHolder_ReturnsBadRequest()
    {
        await InvalidUserRequirements_ReturnsBadRequest(ConfigureValidAuthenticationState, additionalScopes: null, TrnRequirementType.Legacy, HttpMethod.Post, "/sign-in/trn/official-name");
    }

    [Fact]
    public async Task Post_HasTrnNotSet_RedirectsToHasTrnPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified(), CustomScopes.Trn, TrnRequirementType.Legacy);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/official-name?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/has-trn", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_EmptyOfficialFirstName_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy);
        var lastName = "last";

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/official-name?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "OfficialLastName", lastName },
                { "HasPreviousName", AuthenticationState.HasPreviousNameOption.No },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "OfficialFirstName", "Enter your first name");
    }

    [Fact]
    public async Task Post_EmptyOfficialLastName_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy);
        var firstName = "first";

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/official-name?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "OfficialFirstName", firstName },
                { "HasPreviousName", AuthenticationState.HasPreviousNameOption.No },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "OfficialLastName", "Enter your last name");
    }

    [Fact]
    public async Task Post_HasPreviousNameNotSpecified_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy);
        var firstName = "first";
        var lastName = "last";

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/official-name?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "OfficialFirstName", firstName },
                { "OfficialLastName", lastName }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "HasPreviousName", "Select if you have changed your name");
    }

    [Fact]
    public async Task Post_TooLongOfficialFirstName_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy);
        var firstName = new string('a', 201);
        var lastName = "last";

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/official-name?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "OfficialFirstName", firstName },
                { "OfficialLastName", lastName },
                { "HasPreviousName", AuthenticationState.HasPreviousNameOption.No },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "OfficialFirstName", "First name must be 200 characters or less");
    }

    [Fact]
    public async Task Post_TooLongOfficialLastName_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy);
        var firstName = "last";
        var lastName = new string('a', 201);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/official-name?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "OfficialFirstName", firstName },
                { "OfficialLastName", lastName },
                { "HasPreviousName", AuthenticationState.HasPreviousNameOption.No },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "OfficialLastName", "Last name must be 200 characters or less");
    }

    [Fact]
    public async Task Post_ValidOfficialName_SetsOfficialNameOnAuthenticationStateRedirectsToPreferredName()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy);
        var firstName = "first";
        var lastName = "last";

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/official-name?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "OfficialFirstName", firstName },
                { "OfficialLastName", lastName },
                { "HasPreviousName", AuthenticationState.HasPreviousNameOption.No },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/preferred-name", response.Headers.Location?.OriginalString);

        Assert.Equal(firstName, authStateHelper.AuthenticationState.OfficialFirstName);
        Assert.Equal(lastName, authStateHelper.AuthenticationState.OfficialLastName);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Post_HasPreviousOfficialNames_SetsPreviousOfficialNameOnAuthenticationState(bool hasPreviousName)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy);
        var previousFirstName = "previous first";
        var previousLastName = "previous last";

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/official-name?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "OfficialFirstName", "first" },
                { "OfficialLastName", "last" },
                { "PreviousOfficialFirstName", previousFirstName },
                { "PreviousOfficialLastName", previousLastName },
                { "HasPreviousName", hasPreviousName ? AuthenticationState.HasPreviousNameOption.Yes : AuthenticationState.HasPreviousNameOption.No },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);

        if (hasPreviousName)
        {
            Assert.Equal(previousFirstName, authStateHelper.AuthenticationState.PreviousOfficialFirstName);
            Assert.Equal(previousLastName, authStateHelper.AuthenticationState.PreviousOfficialLastName);
        }
        else
        {
            Assert.Null(authStateHelper.AuthenticationState.PreviousOfficialFirstName);
            Assert.Null(authStateHelper.AuthenticationState.PreviousOfficialLastName);
        }
    }

    [Fact]
    public async Task Post_TrnLookupFindsExactlyOneResult_RedirectsToCheckAnswersPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(ConfigureValidAuthenticationState, CustomScopes.Trn, TrnRequirementType.Legacy);
        ConfigureDqtApiClientToReturnSingleMatch(authStateHelper);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/official-name?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "OfficialFirstName", "first" },
                { "OfficialLastName", "last" },
                { "HasPreviousName", AuthenticationState.HasPreviousNameOption.No },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/check-answers", response.Headers.Location?.OriginalString);
    }

    private Func<AuthenticationState, Task> ConfigureValidAuthenticationState(AuthenticationStateHelper.Configure configure) =>
        configure.Trn.HasTrnSet();
}
