using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Account.Phone;

public class ConfirmTests : TestBase
{
    public ConfirmTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_NoPhone_ReturnsBadRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, $"/account/phone/confirm");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsSuccess()
    {
        var mobileNumber = Faker.Phone.Number();
        var protectedMobileNumber = HostFixture.Services.GetRequiredService<ProtectedStringFactory>().CreateFromPlainValue(mobileNumber);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/account/phone/confirm?mobileNumber={UrlEncode(protectedMobileNumber.EncryptedValue)}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_NoPhone_ReturnsBadRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/phone/confirm");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_UnknownPin_ReturnsError()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var newMobileNumber = Faker.Phone.Number();

        // The real PIN generation service never generates pins that start with a '0'
        var pin = "01234";

        var protectedMobilePhone = HostFixture.Services.GetRequiredService<ProtectedStringFactory>().CreateFromPlainValue(newMobileNumber);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/phone/confirm?mobileNumber={UrlEncode(protectedMobilePhone.EncryptedValue)}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pin }
            }
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
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var newMobileNumber = Faker.Phone.Number();
        var pin = "0";

        var protectedMobileNumber = HostFixture.Services.GetRequiredService<ProtectedStringFactory>().CreateFromPlainValue(newMobileNumber);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/phone/confirm?mobileNumber={UrlEncode(protectedMobileNumber.EncryptedValue)}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pin }
            }
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
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var newMobileNumber = Faker.Phone.Number();
        var pin = "0123345678";

        var protectedMobileNumber = HostFixture.Services.GetRequiredService<ProtectedStringFactory>().CreateFromPlainValue(newMobileNumber);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/phone/confirm?mobileNumber={UrlEncode(protectedMobileNumber.EncryptedValue)}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pin }
            }
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
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var newMobileNumber = Faker.Phone.Number();
        var pin = "abc";

        var protectedMobileNumber = HostFixture.Services.GetRequiredService<ProtectedStringFactory>().CreateFromPlainValue(newMobileNumber);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/phone/confirm?mobileNumber={UrlEncode(protectedMobileNumber.EncryptedValue)}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pin }
            }
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
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var newMobileNumber = Faker.Phone.Number();
        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pinResult = await userVerificationService.GenerateSmsPin(newMobileNumber);

        Assert.True(pinResult.Succeeded);
        Clock.AdvanceBy(TimeSpan.FromHours(1));
        SpyRegistry.Get<IUserVerificationService>().Reset();

        var protectedMobileNumber = HostFixture.Services.GetRequiredService<ProtectedStringFactory>().CreateFromPlainValue(newMobileNumber);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/phone/confirm?mobileNumber={UrlEncode(protectedMobileNumber.EncryptedValue)}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pinResult.Pin! }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Code", "The security code has expired. New code sent.");

        HostFixture.UserVerificationService.Verify(mock => mock.GenerateSmsPin(newMobileNumber), Times.Once);
    }

    [Fact]
    public async Task Post_PinExpiredMoreThanTwoHoursAgo_ReturnsErrorAndDoesNotSendANewPin()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var newMobileNumber = Faker.Phone.Number();
        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var userVerificationOptions = HostFixture.Services.GetRequiredService<IOptions<UserVerificationOptions>>();
        var pinResult = await userVerificationService.GenerateSmsPin(newMobileNumber);

        Assert.True(pinResult.Succeeded);
        Clock.AdvanceBy(TimeSpan.FromHours(2) + TimeSpan.FromSeconds(userVerificationOptions.Value.PinLifetimeSeconds));
        SpyRegistry.Get<IUserVerificationService>().Reset();

        var protectedMobileNumber = HostFixture.Services.GetRequiredService<ProtectedStringFactory>().CreateFromPlainValue(newMobileNumber);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/phone/confirm?mobileNumber={UrlEncode(protectedMobileNumber.EncryptedValue)}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pinResult.Pin! }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Code", "Enter a correct security code");

        HostFixture.UserVerificationService.Verify(mock => mock.GenerateSmsPin(newMobileNumber), Times.Never);
    }

    [Fact]
    public async Task Post_ValidForm_UpdatesNameEmitsEventAndRedirects()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var client = TestClients.Client1;
        var redirectUri = client.RedirectUris.First().GetLeftPart(UriPartial.Authority);

        var returnUrl = $"/account?client_id={client.ClientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}";

        var newMobileNumber = Faker.Phone.Number();
        var protectedMobileNumber = HostFixture.Services.GetRequiredService<ProtectedStringFactory>().CreateFromPlainValue(newMobileNumber);

        var userVerificationService = HostFixture.Services.GetRequiredService<IUserVerificationService>();
        var pinResult = await userVerificationService.GenerateSmsPin(newMobileNumber);
        Assert.True(pinResult.Succeeded);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/phone/confirm?mobileNumber={UrlEncode(protectedMobileNumber.EncryptedValue)}&returnUrl={UrlEncode(returnUrl)}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pinResult.Pin! },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(returnUrl, response.Headers.Location?.OriginalString);

        user = await TestData.WithDbContext(dbContext => dbContext.Users.SingleAsync(u => u.UserId == user.UserId));
        Assert.Equal(newMobileNumber, user.MobileNumber);
        Assert.Equal(Clock.UtcNow, user.Updated);

        EventObserver.AssertEventsSaved(
            e =>
            {
                var userUpdatedEvent = Assert.IsType<UserUpdatedEvent>(e);
                Assert.Equal(Clock.UtcNow, userUpdatedEvent.CreatedUtc);
                Assert.Equal(UserUpdatedEventSource.ChangedByUser, userUpdatedEvent.Source);
                Assert.Equal(UserUpdatedEventChanges.MobileNumber, userUpdatedEvent.Changes);
                Assert.Equal(user.UserId, userUpdatedEvent.User.UserId);
            });

        var redirectedResponse = await response.FollowRedirect(HttpClient);
        var redirectedDoc = await redirectedResponse.GetDocument();
        AssertEx.HtmlDocumentHasFlashSuccess(redirectedDoc, "Your mobile number has been updated");
    }

    private static string UrlEncode(string value) => UrlEncoder.Default.Encode(value);
}
