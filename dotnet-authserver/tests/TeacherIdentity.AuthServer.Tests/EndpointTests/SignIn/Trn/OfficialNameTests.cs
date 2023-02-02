namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Trn;

[Collection(nameof(DisableParallelization))]  // Relies on mocks
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
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(HttpMethod.Get, "/sign-in/trn/official-name");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(c => c.EmailVerified(), HttpMethod.Get, "/sign-in/trn/official-name");
    }

    [Fact]
    public async Task Get_NoEmail_RedirectsToEmailPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Start());

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/official-name?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/email", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_EmailNotVerified_RedirectsToEmailConfirmationPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet());

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/official-name?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/email-confirmation", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_EmptyOfficialFirstName_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified());
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/official-name?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
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
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified());
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/official-name?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "OfficialLastName", "Enter your last name");
    }

    [Fact]
    public async Task Post_ValidOfficialName_SetsOfficialNameOnAuthenticationStateRedirectsToPreferredName()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified());
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
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified());
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
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified());
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
}
