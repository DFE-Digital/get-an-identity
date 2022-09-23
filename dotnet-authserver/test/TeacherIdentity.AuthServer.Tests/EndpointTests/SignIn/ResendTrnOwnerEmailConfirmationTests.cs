namespace TeacherIdentity.AuthServer.Tests.EndpointTests.SignIn;

[Collection(nameof(DisableParallelization))]  // Depends on mocks
public class ResendTrnOwnerEmailConfirmationTests : TestBase
{
    public ResendTrnOwnerEmailConfirmationTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Get, "/sign-in/trn/resend-email-confirmation");
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);

        var authStateHelper = CreateAuthenticationStateHelper(email, existingTrnOwner.EmailAddress);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/sign-in/trn/resend-email-confirmation?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Act
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_NoAuthenticationStateProvided_ReturnsBadRequest()
    {
        await InvalidAuthenticationState_ReturnsBadRequest(HttpMethod.Post, "/sign-in/trn/resend-email-confirmation");
    }

    [Fact]
    public async Task Post_ValidRequest_GeneratesPinAndRedirectsToConfirmation()
    {
        // Arrange
        var email = Faker.Internet.Email();
        var existingTrnOwner = await TestData.CreateUser(hasTrn: true);

        var authStateHelper = CreateAuthenticationStateHelper(email, existingTrnOwner.EmailAddress);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/sign-in/trn/resend-email-confirmation?{authStateHelper.ToQueryParam()}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal($"/sign-in/trn/different-email?{authStateHelper.ToQueryParam()}", response.Headers.Location?.OriginalString);

        HostFixture.EmailVerificationService.Verify(mock => mock.GeneratePin(existingTrnOwner.EmailAddress), Times.Once);
    }

    private AuthenticationStateHelper CreateAuthenticationStateHelper(string email, string existingTrnOwnerEmail) =>
        CreateAuthenticationStateHelper(authState =>
        {
            authState.OnEmailSet(email);
            authState.OnEmailVerified(user: null);
            authState.OnTrnLookupCompletedForTrnAlreadyInUse(existingTrnOwnerEmail);
        });
}
