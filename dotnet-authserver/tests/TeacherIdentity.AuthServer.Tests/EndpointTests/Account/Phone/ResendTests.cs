using System.Text.Encodings.Web;
using TeacherIdentity.AuthServer.Helpers;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Account.Phone;

public class ResendTests : TestBase
{
    private readonly ProtectedString _mobileNumber;

    public ResendTests(HostFixture hostFixture)
        : base(hostFixture)
    {
        _mobileNumber = HostFixture.Services.GetRequiredService<ProtectedStringFactory>().CreateFromPlainValue(Faker.Phone.Number());
    }

    [Fact]
    public async Task Post_EmptyMobileNumber_ReturnsError()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/phone/resend?mobileNumber={_mobileNumber}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "NewMobileNumber", "Enter your new mobile phone number");
    }

    [Theory]
    [MemberData(nameof(InvalidPhoneData))]
    public async Task Post_InvalidMobileNumber_RendersError(string newMobileNumber, string expectedErrorMessage)
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/phone/resend?mobileNumber={_mobileNumber}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "NewMobileNumber", newMobileNumber }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "NewMobileNumber", expectedErrorMessage);
    }

    [Fact]
    public async Task Post_MobileNumberInUse_RendersError()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var anotherUser = await TestData.CreateUser();
        var newMobileNumber = anotherUser.MobileNumber;

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/phone/resend?mobileNumber={_mobileNumber}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "NewMobileNumber", newMobileNumber! }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "NewMobileNumber", "This mobile phone number is already in use - Enter a different mobile phone number");
    }

    [Fact]
    public async Task Post_MobileNumberMatchesCurrentMobileNumber_DoesNotProduceError()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/phone/resend?mobileNumber={_mobileNumber}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "NewMobileNumber", user.MobileNumber! }
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

        var newMobileNumber = Faker.Phone.Number();
        var formattedMobileNumber = PhoneHelper.FormatMobileNumber(newMobileNumber);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/phone/resend?mobileNumber={_mobileNumber}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "NewMobileNumber", newMobileNumber }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/account/phone/confirm", response.Headers.Location?.OriginalString);

        HostFixture.UserVerificationService.Verify(mock => mock.GenerateSmsPin(formattedMobileNumber), Times.Once);
    }

    [Fact]
    public async Task Post_ValidRequest_RedirectsWithCorrectReturnUrl()
    {
        // Arrange
        var client = TestClients.Client1;
        var redirectUri = client.RedirectUris.First().GetLeftPart(UriPartial.Authority);

        var returnUrl = UrlEncoder.Default.Encode($"/account?client_id={client.ClientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}");

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/phone/resend?mobileNumber={_mobileNumber}&returnUrl={returnUrl}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "NewMobileNumber", Faker.Phone.Number() },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Contains($"returnUrl={returnUrl}", response.Headers.Location?.OriginalString);
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

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/phone/resend?mobileNumber={_mobileNumber}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "NewMobileNumber", newMobileNumber }
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
