using System.Text.Encodings.Web;
using Flurl;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Authenticated.UpdateEmail;

[Collection(nameof(DisableParallelization))]  // Relies on mocks
public class IndexTests : TestBase
{
    public IndexTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_RendersPageSuccessfully()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var returnUrl = "/_tests/empty";

        var request = new HttpRequestMessage(HttpMethod.Get, $"/update-email?returnUrl={UrlEncoder.Default.Encode(returnUrl)}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(InvalidEmailData))]
    public async Task Post_InvalidEmail_RendersError(string newEmail, string expectedErrorMessage)
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var returnUrl = "/_tests/empty";

        var request = new HttpRequestMessage(HttpMethod.Post, $"/update-email?returnUrl={UrlEncoder.Default.Encode(returnUrl)}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", newEmail }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Email", expectedErrorMessage);
    }

    [Fact]
    public async Task Post_EmailInUse_RendersError()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var anotherUser = await TestData.CreateUser();
        var newEmail = anotherUser.EmailAddress;

        var returnUrl = "/_tests/empty";

        var request = new HttpRequestMessage(HttpMethod.Post, $"/update-email?returnUrl={UrlEncoder.Default.Encode(returnUrl)}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", newEmail }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Email", "This email address is already in use - Enter a different email address");
    }

    [Fact]
    public async Task Post_EmailMatchesCurrentEmail_DoesNotProduceError()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var returnUrl = "/_tests/empty";

        var request = new HttpRequestMessage(HttpMethod.Post, $"/update-email?returnUrl={UrlEncoder.Default.Encode(returnUrl)}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", user.EmailAddress }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.True((int)response.StatusCode < 400);
    }

    [Fact]
    public async Task Post_ValidRequest_GeneratesPinAndRedirects()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var newEmail = Faker.Internet.Email();

        var returnUrl = "/_tests/empty";

        var request = new HttpRequestMessage(HttpMethod.Post, $"/update-email?returnUrl={UrlEncoder.Default.Encode(returnUrl)}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", newEmail }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal("/update-email/confirmation", new Url(response.Headers.Location?.OriginalString).Path);

        HostFixture.EmailVerificationService.Verify(mock => mock.GeneratePin(newEmail), Times.Once);
    }

    [Fact]
    public async Task Post_PinGenerationRateLimitedExceeded_ReturnsTooManyRequests()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        HostFixture.RateLimitStore
            .Setup(x => x.IsClientIpBlockedForPinGeneration(TestRequestClientIpProvider.ClientIpAddress))
            .ReturnsAsync(true);

        var newEmail = Faker.Internet.Email();

        var returnUrl = "/_tests/empty";

        var request = new HttpRequestMessage(HttpMethod.Post, $"/update-email?returnUrl={UrlEncoder.Default.Encode(returnUrl)}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", newEmail }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status429TooManyRequests, (int)response.StatusCode);
    }

    public static TheoryData<string, string> InvalidEmailData { get; } = new()
    {
        { "", "Enter your new email address" },
        { "xx", "Enter a valid email address" }
    };
}
