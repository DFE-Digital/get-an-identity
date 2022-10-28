using System.Text.Encodings.Web;
using Flurl;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Authenticated.UpdateEmail;

[Collection(nameof(DisableParallelization))]  // Relies on mocks
public class ResendConfirmationTests : TestBase
{
    public ResendConfirmationTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_NoEmail_ReturnsBadRequest()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var returnUrl = "/_tests/empty";

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/update-email/resend-confirmation?returnUrl={UrlEncode(returnUrl)}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsSuccess()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var newEmail = Faker.Internet.Email();

        var protectedEmail = HostFixture.Services.GetRequiredService<ProtectedStringFactory>().CreateFromPlainValue(newEmail);

        var returnUrl = "/_tests/empty";

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"/update-email/resend-confirmation?email={UrlEncode(protectedEmail.EncryptedValue)}&returnUrl={UrlEncode(returnUrl)}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_NoEmail_ReturnsBadRequest()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var returnUrl = "/_tests/empty";

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/update-email/resend-confirmation?returnUrl={UrlEncode(returnUrl)}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_RateLimitExceeded_ReturnsTooManyRequests()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        HostFixture.RateLimitStore
            .Setup(x => x.IsClientIpBlockedForPinGeneration(TestRequestClientIpProvider.ClientIpAddress))
            .ReturnsAsync(true);

        var newEmail = Faker.Internet.Email();

        var protectedEmail = HostFixture.Services.GetRequiredService<ProtectedStringFactory>().CreateFromPlainValue(newEmail);

        var returnUrl = "/_tests/empty";

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/update-email/resend-confirmation?email={UrlEncode(protectedEmail.EncryptedValue)}&returnUrl={UrlEncode(returnUrl)}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status429TooManyRequests, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidRequest_GeneratesPinAndRedirectsToConfirmation()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var newEmail = Faker.Internet.Email();

        var protectedEmail = HostFixture.Services.GetRequiredService<ProtectedStringFactory>().CreateFromPlainValue(newEmail);

        var returnUrl = "/_tests/empty";

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/update-email/resend-confirmation?email={UrlEncode(protectedEmail.EncryptedValue)}&returnUrl={UrlEncode(returnUrl)}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal("/update-email/confirmation", new Url(response.Headers.Location?.OriginalString).Path);

        HostFixture.EmailVerificationService.Verify(mock => mock.GeneratePin(newEmail), Times.Once);
    }

    private static string UrlEncode(string value) => UrlEncoder.Default.Encode(value);
}
