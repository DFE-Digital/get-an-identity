using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

[Collection(nameof(DisableParallelization))]  // Depends on mocks and changes the clock
public class EmailConfirmationTests : TestBase
{
    public EmailConfirmationTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/email-confirmation");
    }

    [Fact]
    public async Task Get_EmailAlreadyVerified_RedirectsToNextPage()
    {
        // Arrange
        var authStateHelper = CreateAuthenticationStateHelper(authState =>
        {
            authState.OnEmailSet(Faker.Internet.Email());
            authState.OnEmailVerified(user: null);
        });
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var authStateHelper = CreateAuthenticationStateHelper();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal(authStateHelper.AuthenticationState.EmailAddress, doc.GetElementByTestId("email")?.TextContent);
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/email-confirmation");
    }

    [Fact]
    public async Task Post_UnknownPin_ReturnsError()
    {
        // Arrange
        var email = Faker.Internet.Email();

        // The real PIN generation service never generates pins that start with a '0'
        var pin = "01234";

        var authStateHelper = CreateAuthenticationStateHelper(email);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Code", pin)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Code", "Enter a correct security code");
    }

    [Fact]
    public async Task Post_PinTooShort_ReturnsError()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var pin = "0";

        var authStateHelper = CreateAuthenticationStateHelper(email);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Code", pin)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Code", "You’ve not entered enough numbers, the code must be 5 numbers");
    }

    [Fact]
    public async Task Post_PinTooLong_ReturnsError()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var pin = "0123345678";

        var authStateHelper = CreateAuthenticationStateHelper(email);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Code", pin)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Code", "You’ve entered too many numbers, the code must be 5 numbers");
    }

    [Fact]
    public async Task Post_NonNumericPin_ReturnsError()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var pin = "abc";

        var authStateHelper = CreateAuthenticationStateHelper(email);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Code", pin)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Code", "The code must be 5 numbers");
    }

    [Fact]
    public async Task Post_PinExpiredLessThanTwoHoursAgo_ReturnsErrorAndSendsANewPin()
    {
        // Arrange
        var email = Faker.Internet.Email();

        var emailVerificationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var pinResult = await emailVerificationService.GeneratePin(email);
        Clock.AdvanceBy(TimeSpan.FromHours(1));
        Spy.Get(emailVerificationService).Reset();

        var authStateHelper = CreateAuthenticationStateHelper(email);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Code", pinResult.Pin!)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Code", "The security code has expired. New code sent.");

        HostFixture.EmailVerificationService.Verify(mock => mock.GeneratePin(email), Times.Once);
    }

    [Fact]
    public async Task Post_PinExpiredMoreThanTwoHoursAgo_ReturnsErrorAndDoesNotSendANewPin()
    {
        // Arrange
        var email = Faker.Internet.Email();

        var emailVerificationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var emailVerificationOptions = HostFixture.Services.GetRequiredService<IOptions<EmailVerificationOptions>>();
        var pinResult = await emailVerificationService.GeneratePin(email);
        Clock.AdvanceBy(TimeSpan.FromHours(2) + TimeSpan.FromSeconds(emailVerificationOptions.Value.PinLifetimeSeconds));
        Spy.Get(emailVerificationService).Reset();

        var authStateHelper = CreateAuthenticationStateHelper(email);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Code", pinResult.Pin!)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Code", "Enter a correct security code");

        HostFixture.EmailVerificationService.Verify(mock => mock.GeneratePin(email), Times.Never);
    }

    [Fact]
    public async Task Post_ValidPinForNewUser_UpdatesAuthenticationStateAndRedirects()
    {
        // Arrange
        var email = Faker.Internet.Email();

        var emailVerificationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var pinResult = await emailVerificationService.GeneratePin(email);

        var authStateHelper = CreateAuthenticationStateHelper(email);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Code", pinResult.Pin!)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);

        Assert.True(authStateHelper.AuthenticationState.EmailAddressVerified);
        Assert.True(authStateHelper.AuthenticationState.FirstTimeSignInForEmail);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Post_ValidPinForKnownUserWithTrn_UpdatesAuthenticationStateSignsInAndRedirects(bool hasTrn)
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: hasTrn);

        var emailVerificationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var pinResult = await emailVerificationService.GeneratePin(user.EmailAddress);

        var authStateHelper = CreateAuthenticationStateHelper(user.EmailAddress);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Code", pinResult.Pin!)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);

        Assert.True(authStateHelper.AuthenticationState.EmailAddressVerified);
        Assert.NotNull(authStateHelper.AuthenticationState.UserId);
        Assert.False(authStateHelper.AuthenticationState.FirstTimeSignInForEmail);
        Assert.Equal(user.Trn, authStateHelper.AuthenticationState.Trn);
    }

    [Fact]
    public async Task Post_ValidPinForAdminScopeWithUnknownUser_ReturnsForbidden()
    {
        // Arrange
        var email = Faker.Internet.Email();

        var emailVerificationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var pinResult = await emailVerificationService.GeneratePin(email);

        var authStateHelper = CreateAuthenticationStateHelper(email, scope: CustomScopes.GetAnIdentityAdmin);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Code", pinResult.Pin!)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidPinForAdminScopeWithNonAdminUser_ReturnsForbidden()
    {
        // Arrange
        HostFixture.RateLimitStore.Setup(x => x.IsClientIpBlockedForPinVerification(It.IsAny<string>())).Returns(Task.FromResult(false));
        var user = await TestData.CreateUser(userType: Models.UserType.Default);

        var emailVerificationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var pinResult = await emailVerificationService.GeneratePin(user.EmailAddress);

        var authStateHelper = CreateAuthenticationStateHelper(user.EmailAddress, scope: CustomScopes.GetAnIdentityAdmin);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Code", pinResult.Pin!)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidPinForAdminScopeForKnownUserWithTrn_UpdatesAuthenticationStateSignsInAndRedirects()
    {
        // Arrange
        HostFixture.RateLimitStore.Setup(x => x.IsClientIpBlockedForPinVerification(It.IsAny<string>())).Returns(Task.FromResult(false));
        var user = await TestData.CreateUser(userType: Models.UserType.Staff);
        var emailVerificationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var pinResult = await emailVerificationService.GeneratePin(user.EmailAddress);

        var authStateHelper = CreateAuthenticationStateHelper(user.EmailAddress, scope: CustomScopes.GetAnIdentityAdmin);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Code", pinResult.Pin!)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);

        Assert.True(authStateHelper.AuthenticationState.EmailAddressVerified);
        Assert.NotNull(authStateHelper.AuthenticationState.UserId);
        Assert.False(authStateHelper.AuthenticationState.FirstTimeSignInForEmail);

        EventObserver.AssertEventsSaved(
            e =>
            {
                var userSignedInEvent = Assert.IsType<UserSignedIn>(e);
                Assert.Equal(Clock.UtcNow, userSignedInEvent.CreatedUtc);
                Assert.Equal(authStateHelper.AuthenticationState.OAuthState?.ClientId, userSignedInEvent.ClientId);
                Assert.Equal(authStateHelper.AuthenticationState.OAuthState?.Scope, userSignedInEvent.Scope);
                Assert.Equal(user.UserId, userSignedInEvent.UserId);
            });
    }

    [Fact]
    public async Task Post_BlockedClient_ReturnsTooManyRequestsStatusCode()
    {
        // Arrange
        HostFixture.RateLimitStore.Setup(x => x.IsClientIpBlockedForPinVerification(It.IsAny<string>())).Returns(Task.FromResult(true));
        var email = Faker.Internet.Email();
        var emailVerificationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var pinResult = await emailVerificationService.GeneratePin(email);

        var authStateHelper = CreateAuthenticationStateHelper(email);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Code", pinResult.Pin!)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status429TooManyRequests, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidPinForNonAdminScopeWithAdminUser_ReturnsForbidden()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Staff);

        var emailVerificationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var pinResult = await emailVerificationService.GeneratePin(user.EmailAddress);

        var authStateHelper = CreateAuthenticationStateHelper(user.EmailAddress, scope: CustomScopes.Trn);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Code", pinResult.Pin!)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, (int)response.StatusCode);
    }

    private AuthenticationStateHelper CreateAuthenticationStateHelper(string? email = null, string scope = "trn") =>
        CreateAuthenticationStateHelper(
            authState =>
            {
                authState.OnEmailSet(email ?? Faker.Internet.Email());
            },
            scope);
}
