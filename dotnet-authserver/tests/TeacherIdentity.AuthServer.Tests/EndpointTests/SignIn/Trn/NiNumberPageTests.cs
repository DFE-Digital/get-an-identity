namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Trn;

[Collection(nameof(DisableParallelization))]  // Relies on mocks
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
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(HttpMethod.Get, "/sign-in/trn/ni-number");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(c => c.OfficialNameSet(), HttpMethod.Get, "/sign-in/trn/ni-number");
    }

    [Fact]
    public async Task Get_NoEmail_RedirectsToEmailPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Start());

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/have-nino?{authStateHelper.ToQueryParam()}");

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

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/have-nino?{authStateHelper.ToQueryParam()}");

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

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/have-nino?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersContent()
    {
        await ValidRequest_RendersContent("/sign-in/trn/ni-number", c => c.OfficialNameSet());
    }

    [Fact]
    public async Task Post_EmptyNiNumber_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.OfficialNameSet());

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
    [InlineData("QQ 123456C")]
    [InlineData("123456")]
    [InlineData("QQ12345C")]
    public async Task Post_InvalidNiNumber_ReturnsError(string niNumber)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.OfficialNameSet());

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
    [InlineData("QQ 12 34 56 C")]
    [InlineData("QQ123456C")]
    [InlineData("qQ123456c")]
    public async Task Post_ValidNiNumber_SetsNiNumberOnAuthenticationStateRedirectsToAwardedQtsPage(string niNumber)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.OfficialNameSet());
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
    public async Task Post_NiNumberNotKnown_Returns302Found()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.OfficialNameSet());
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
    }

    [Fact]
    public async Task Post_TrnLookupFindsExactlyOneResult_RedirectsToCheckAnswersPage()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.OfficialNameSet());
        ConfigureDqtApiClientToReturnSingleMatch(authStateHelper);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/ni-number?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "NiNumber", "QQ123456C" },
                { "submit", "submit" },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/trn/check-answers", response.Headers.Location?.OriginalString);
    }
}
