namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

public class ResendEmailConfirmationTests : TestBase
{
    public ResendEmailConfirmationTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/resend-email-confirmation");
    }

    [Fact]
    public async Task Get_EmailNotKnown_RedirectsToEmailPage()
    {
        // Arrange
        var authStateHelper = CreateAuthenticationStateHelper(authState => authState.EmailAddress = null);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/resend-email-confirmation?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/sign-in/email?{authStateHelper.ToQueryParam()}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_EmailAlreadyVerified_RedirectsToNextPage()
    {
        // Arrange
        var authStateHelper = CreateAuthenticationStateHelper(authState =>
        {
            authState.EmailAddress = Faker.Internet.Email();
            authState.EmailAddressVerified = true;
        });

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/resend-email-confirmation?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var authStateHelper = CreateAuthenticationStateHelper();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/resend-email-confirmation?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal(authStateHelper.AuthenticationState.EmailAddress, doc.GetElementById("Email")?.GetAttribute("value"));
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/resend-email-confirmation");
    }

    [Fact]
    public async Task Post_EmailNotKnown_RedirectsToEmailPage()
    {
        // Arrange
        var authStateHelper = CreateAuthenticationStateHelper(authState => authState.EmailAddress = null);
        var differentEmail = Faker.Internet.Email();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/resend-email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Email", differentEmail)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/sign-in/email?{authStateHelper.ToQueryParam()}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_EmailAlreadyVerified_RedirectsToNextPage()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var authStateHelper = CreateAuthenticationStateHelper(authState =>
        {
            authState.EmailAddress = email;
            authState.EmailAddressVerified = true;
        });

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/resend-email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Email", email)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.NotEqual($"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Post_EmptyEmail_ReturnsError()
    {
        // Arrange
        var authStateHelper = CreateAuthenticationStateHelper();
        var differentEmail = "";

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/resend-email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Email", differentEmail)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.ResponseHasError(response, "Email", "Enter your email address");
    }

    [Fact]
    public async Task Post_InvalidEmail_ReturnsError()
    {
        // Arrange
        var authStateHelper = CreateAuthenticationStateHelper();
        var differentEmail = "xx";

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/resend-email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Email", differentEmail)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.ResponseHasError(response, "Email", "Enter a valid email address");
    }

    [Fact]
    public async Task Post_ValidRequest_SetsEmailOnAuthenticationStateGeneratesPinAndRedirectsToConfirmation()
    {
        // Arrange
        var authStateHelper = CreateAuthenticationStateHelper();
        var differentEmail = Faker.Internet.Email();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/resend-email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Email", differentEmail)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}", response.Headers.Location?.OriginalString);

        Assert.Equal(differentEmail, authStateHelper.AuthenticationState.EmailAddress);

        A.CallTo(() => HostFixture.EmailVerificationService!.GeneratePin(differentEmail)).MustHaveHappenedOnceExactly();
    }

    private AuthenticationStateHelper CreateAuthenticationStateHelper() =>
        CreateAuthenticationStateHelper(authState =>
        {
            authState.EmailAddress = Faker.Internet.Email();
        });
}