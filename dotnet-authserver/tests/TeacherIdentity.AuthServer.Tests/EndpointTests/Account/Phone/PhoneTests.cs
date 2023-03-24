using TeacherIdentity.AuthServer.Helpers;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Account.Phone;

public class PhoneTests : TestBase
{
    public PhoneTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Post_EmptyMobileNumber_ReturnsError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/account/phone")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "MobileNumber", "Enter your new mobile phone number");
    }

    [Theory]
    [MemberData(nameof(InvalidPhoneData))]
    public async Task Post_InvalidMobileNumber_RendersError(string newMobileNumber, string expectedErrorMessage)
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/phone")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "MobileNumber", newMobileNumber }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "MobileNumber", expectedErrorMessage);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Post_MobileNumberInUse_RendersError(bool isOwnNumber)
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var anotherUser = await TestData.CreateUser();
        var newMobileNumber = isOwnNumber ? user.MobileNumber : anotherUser.MobileNumber;

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/phone")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "MobileNumber", newMobileNumber! }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        var expectedMessage = isOwnNumber
            ? "Enter a different mobile phone number. The one you’ve entered is the same as the one already on your account"
            : "This mobile phone number is already in use - Enter a different mobile phone number";

        await AssertEx.HtmlResponseHasError(response, "MobileNumber", expectedMessage);
    }

    [Fact]
    public async Task Post_ValidRequest_GeneratesPinAndRedirects()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var newMobileNumber = Faker.Phone.Number();
        var formattedMobileNumber = PhoneHelper.FormatMobileNumber(newMobileNumber);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/phone")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "MobileNumber", newMobileNumber }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith($"/account/phone/confirm", response.Headers.Location?.OriginalString);

        HostFixture.UserVerificationService.Verify(mock => mock.GenerateSmsPin(formattedMobileNumber), Times.Once);
    }

    [Fact]
    public async Task Post_ValidRequest_RedirectsWithClientRedirectInfo()
    {
        // Arrange
        var clientRedirectInfo = CreateClientRedirectInfo();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/phone?{clientRedirectInfo.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "MobileNumber", Faker.Phone.Number() },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Contains(clientRedirectInfo.ToQueryParam(), response.Headers.Location?.OriginalString);
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

        var newMobileNumber = Faker.Phone.Number();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/phone")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "MobileNumber", newMobileNumber }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status429TooManyRequests, (int)response.StatusCode);
    }

    public static TheoryData<string, string> InvalidPhoneData { get; } = new()
    {
        { "", "Enter your new mobile phone number" },
        { "xx", "Enter a valid mobile phone number" }
    };
}
