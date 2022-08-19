using Flurl;
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
    public async Task Post_InvalidPin_ReturnsError()
    {
        // Arrange
        var email = Faker.Internet.Email();

        // The real PIN generation service never generates pins that start with a '0'
        var pin = "012345";

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
        await AssertEx.ResponseHasError(response, "Code", "TODO content: Code is incorrect or expired");
    }

    [Fact]
    public async Task Post_ValidPinForNewUser_UpdatesAuthenticationStateAndRedirects()
    {
        // Arrange
        var email = Faker.Internet.Email();

        var emailConfirmationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var pin = await emailConfirmationService.GeneratePin(email);

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

        var emailConfirmationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var pin = await emailConfirmationService.GeneratePin(user.EmailAddress);

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
