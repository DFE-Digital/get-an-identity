using Flurl;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

public class EmailTests : TestBase
{
    public EmailTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/email");
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsOk()
    {
        // Arrange
        var authStateHelper = CreateAuthenticationStateHelper();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/email?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/email");
    }

    [Fact]
    public async Task Post_EmptyEmail_ReturnsError()
    {
        // Arrange
        var authStateHelper = CreateAuthenticationStateHelper();
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder().ToContent()
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
        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Email", "xxx")
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.ResponseHasError(response, "Email", "Enter a valid email address");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Post_ValidEmail_SetsEmailOnAuthenticationStateGeneratesPinAndRedirectsToConfirmation(bool emailIsKnown)
    {
        // Arrange
        var authStateHelper = CreateAuthenticationStateHelper();
        var email = Faker.Internet.Email();

        if (emailIsKnown)
        {
            await TestData.CreateUser(email);
        }

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/email?{authStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Email", email)
                .ToContent()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal("/sign-in/email-confirmation", new Url(response.Headers.Location).Path);

        Assert.Equal(email, authStateHelper.AuthenticationState.EmailAddress);

        A.CallTo(() => HostFixture.EmailConfirmationService!.GeneratePin(email)).MustHaveHappenedOnceExactly()
            .Then(A.CallTo(() => HostFixture.EmailSender!.SendEmailAddressConfirmationEmail(email, A<string>.Ignored)).MustHaveHappenedOnceExactly());
    }
}
