namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn.Register;

public class NameTests : TestBase
{
    public NameTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/register/name");
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Get, $"/sign-in/register/name");
    }

    [Fact]
    public async Task Get_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/name");
    }

    [Fact]
    public async Task Get_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(), additionalScopes: null, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/name");
    }

    [Fact]
    public async Task Get_MobileNumberNotVerified_RedirectsToPhoneConfirmation()
    {
        await GivenAuthenticationState_RedirectsTo(_previousPageAuthenticationState(), additionalScopes: null, trnRequirementType: null, HttpMethod.Get, "/sign-in/register/name", "/sign-in/register/phone-confirmation");
    }

    [Fact]
    public async Task Get_ValidRequest_RendersContent()
    {
        await ValidRequest_RendersContent(_currentPageAuthenticationState(), additionalScopes: null, trnRequirementType: null, url: "/sign-in/register/name");
    }

    [Fact]
    public async Task Post_InvalidAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/register/name");
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await MissingAuthenticationState_ReturnsBadRequest(HttpMethod.Post, $"/sign-in/register/name");
    }

    [Fact]
    public async Task Post_JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl()
    {
        await JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(additionalScopes: null, trnRequirementType: null, HttpMethod.Post, "/sign-in/register/name");
    }

    [Fact]
    public async Task Post_JourneyHasExpired_RendersErrorPage()
    {
        await JourneyHasExpired_RendersErrorPage(_currentPageAuthenticationState(), additionalScopes: null, trnRequirementType: null, HttpMethod.Post, "/sign-in/register/name");
    }

    [Fact]
    public async Task Post_MobileNumberNotVerified_RedirectsToPhoneConfirmation()
    {
        await GivenAuthenticationState_RedirectsTo(_previousPageAuthenticationState(), additionalScopes: null, trnRequirementType: null, HttpMethod.Post, "/sign-in/register/name", "/sign-in/register/phone-confirmation");
    }

    [Fact]
    public async Task Post_EmptyFirstName_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null, trnRequirementType: null);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/name?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "LastName", Faker.Name.Last() },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "FirstName", "Enter your first name");
    }

    [Fact]
    public async Task Post_EmptyLastName_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/name?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "FirstName", Faker.Name.First() },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "LastName", "Enter your last name");
    }

    [Fact]
    public async Task Post_TooLongFirstName_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/name?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "FirstName", new string('a', 201) },
                { "LastName", Faker.Name.Last() },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "FirstName", "First name must be 200 characters or less");
    }

    [Fact]
    public async Task Post_TooLongMiddleName_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/name?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "FirstName", Faker.Name.First() },
                { "MiddleName", new string('a', 201) },
                { "LastName", Faker.Name.Last() },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "MiddleName", "Middle name must be 200 characters or less");
    }

    [Fact]
    public async Task Post_TooLongLastName_ReturnsError()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/name?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "FirstName", Faker.Name.First() },
                { "LastName", new string('a', 201) },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "LastName", "Last name must be 200 characters or less");
    }

    [Fact]
    public async Task Post_ValidName_SetsNameOnAuthenticationStateAndRedirects()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_currentPageAuthenticationState(), additionalScopes: null);
        var firstName = Faker.Name.First();
        var middleName = Faker.Name.Middle();
        var lastName = Faker.Name.Last();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/name?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "FirstName", firstName },
                { "MiddleName", middleName },
                { "LastName", lastName },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/register/preferred-name", response.Headers.Location?.OriginalString);

        Assert.Equal(firstName, authStateHelper.AuthenticationState.FirstName);
        Assert.Equal(lastName, authStateHelper.AuthenticationState.LastName);
    }

    [Fact]
    public async Task Post_ValidNameAllQuestionsAnswered_RedirectsToCheckAnswers()
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(_allQuestionsAnsweredAuthenticationState(), additionalScopes: null);
        var firstName = Faker.Name.First();
        var lastName = Faker.Name.Last();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/register/name?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "FirstName", firstName },
                { "LastName", lastName },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/register/check-answers", response.Headers.Location?.OriginalString);
    }

    private readonly AuthenticationStateConfigGenerator _currentPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.Name);
    private readonly AuthenticationStateConfigGenerator _previousPageAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.PhoneConfirmation);
    private readonly AuthenticationStateConfigGenerator _allQuestionsAnsweredAuthenticationState = RegisterJourneyAuthenticationStateHelper.ConfigureAuthenticationStateForPage(RegisterJourneyPage.CheckAnswers);
}
