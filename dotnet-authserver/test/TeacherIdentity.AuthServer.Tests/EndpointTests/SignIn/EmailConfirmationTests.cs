using Flurl;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

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
        await AssertEx.ResponseHasError(response, "Code", "Enter a correct security code");
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
        await AssertEx.ResponseHasError(response, "Code", "You’ve not entered enough numbers, the code must be 5 numbers");
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
        await AssertEx.ResponseHasError(response, "Code", "You’ve entered too many numbers, the code must be 5 numbers");
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
        await AssertEx.ResponseHasError(response, "Code", "The code must be 5 numbers");
    }

    [Fact]
    public async Task Post_PinExpiredLessThanTwoHoursAgo_ReturnsErrorAndSendsANewPin()
    {
        // Arrange
        var email = Faker.Internet.Email();

        var emailVerificationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var pin = await emailVerificationService.GeneratePin(email);
        Clock.AdvanceBy(TimeSpan.FromHours(1));
        Fake.ClearRecordedCalls(emailVerificationService);

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
        await AssertEx.ResponseHasError(response, "Code", "The security code has expired. New code sent.");

        A.CallTo(() => HostFixture.EmailVerificationService!.GeneratePin(email)).MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task Post_PinExpiredMoreThanTwoHoursAgo_ReturnsErrorAndDoesNotSendANewPin()
    {
        // Arrange
        var email = Faker.Internet.Email();

        var emailVerificationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var emailVerificationOptions = HostFixture.Services.GetRequiredService<IOptions<EmailVerificationOptions>>();
        var pin = await emailVerificationService.GeneratePin(email);
        Clock.AdvanceBy(TimeSpan.FromHours(2) + TimeSpan.FromSeconds(emailVerificationOptions.Value.PinLifetimeSeconds));
        Fake.ClearRecordedCalls(emailVerificationService);

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
        await AssertEx.ResponseHasError(response, "Code", "Enter a correct security code");

        A.CallTo(() => HostFixture.EmailVerificationService!.GeneratePin(email)).MustNotHaveHappened();
    }

    [Fact]
    public async Task Post_ValidPinForNewUser_UpdatesAuthenticationStateAndRedirects()
    {
        // Arrange
        var email = Faker.Internet.Email();

        var emailVerificationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var pin = await emailVerificationService.GeneratePin(email);

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
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal("/sign-in/trn", new Url(response.Headers.Location).Path);

        Assert.True(authStateHelper.AuthenticationState.EmailAddressVerified);
        Assert.True(authStateHelper.AuthenticationState.FirstTimeUser);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Post_ValidPinForKnownUserWithTrn_UpdatesAuthenticationStateSignsInAndRedirects(bool hasTrn)
    {
        // Arrange
        var user = await TestData.CreateUser();
        var trn = hasTrn ? TestData.GenerateTrn() : null;

        A.CallTo(() => HostFixture.DqtApiClient!.GetTeacherIdentityInfo(user.UserId))
            .Returns(hasTrn ? new DqtTeacherIdentityInfo() { Trn = trn! } : null);

        var emailVerificationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var pin = await emailVerificationService.GeneratePin(user.EmailAddress);

        var authStateHelper = CreateAuthenticationStateHelper(user.EmailAddress);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Code", pin)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal("/connect/authorize", new Url(response.Headers.Location).Path);

        Assert.True(authStateHelper.AuthenticationState.EmailAddressVerified);
        Assert.NotNull(authStateHelper.AuthenticationState.UserId);
        Assert.False(authStateHelper.AuthenticationState.FirstTimeUser);
        Assert.Equal(trn, authStateHelper.AuthenticationState.Trn);
    }

    private AuthenticationStateHelper CreateAuthenticationStateHelper(string? email = null) =>
        CreateAuthenticationStateHelper(authState =>
        {
            authState.EmailAddress = email ?? Faker.Internet.Email();
        });
}
