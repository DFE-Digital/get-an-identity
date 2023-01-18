namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Trn;

public class PreferredNameTests : TestBase
{
    public PreferredNameTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn/preferred-name");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/trn/preferred-name");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(HttpMethod.Get, "/sign-in/trn/preferred-name");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(c => c.OfficialNameSet(), HttpMethod.Get, "/sign-in/trn/preferred-name");
    }

    [Fact]
    public async Task Get_NoEmail_RedirectsToEmailPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Start());

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/preferred-name?{authStateHelper.ToQueryParam()}");

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

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/preferred-name?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/email-confirmation", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_OfficialNameNotSet_RedirectsToNextHopUrl()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified());

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/preferred-name?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_NullHasPreferredName_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.OfficialNameSet());
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/preferred-name?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "HasPreferredName", "Tell us if this is your preferred name");
    }

    [Fact]
    public async Task Post_EmptyPreferredFirstName_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.OfficialNameSet());
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/preferred-name?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasPreferredName", true },
                { "PreferredLastName", "last" },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "PreferredFirstName", "Enter your preferred first name");
    }

    [Fact]
    public async Task Post_EmptyPreferredLastName_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.OfficialNameSet());
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/preferred-name?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasPreferredName", true },
                { "PreferredFirstName", "first" },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "PreferredLastName", "Enter your preferred last name");
    }

    [Fact]
    public async Task Post_ValidForm_RedirectsToDateOfBirthPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.OfficialNameSet());

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/preferred-name?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "HasPreferredName", false },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/date-of-birth", response.Headers.Location?.OriginalString);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Post_HasPreferredName_SetsNameOnAuthenticationState(bool hasPreferredName)
    {
        // Arrange
        var initialFirstName = "first";
        var initialLastName = "last";
        var preferredFirstName = "preferred first";
        var preferredLastName = "preferred last";

        var authStateHelper = await CreateAuthenticationStateHelper(c => c.OfficialNameSet());
        authStateHelper.AuthenticationState.OnNameChanged(initialFirstName, initialLastName);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/preferred-name?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "PreferredFirstName", preferredFirstName },
                { "PreferredLastName", preferredLastName },
                { "HasPreferredName", hasPreferredName},
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);

        if (hasPreferredName)
        {
            Assert.Equal(preferredFirstName, authStateHelper.AuthenticationState.FirstName);
            Assert.Equal(preferredLastName, authStateHelper.AuthenticationState.LastName);
        }
        else
        {
            Assert.Equal(initialFirstName, authStateHelper.AuthenticationState.FirstName);
            Assert.Equal(initialLastName, authStateHelper.AuthenticationState.LastName);
        }
    }
}
