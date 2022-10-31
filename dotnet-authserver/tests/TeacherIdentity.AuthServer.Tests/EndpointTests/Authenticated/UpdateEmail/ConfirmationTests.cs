using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Authenticated.UpdateEmail;

[Collection(nameof(DisableParallelization))]  // Depends on mocks and changes the clock
public class ConfirmationTests : TestBase
{
    public ConfirmationTests(HostFixture hostFixture)
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
            $"/update-email/confirmation?returnUrl={UrlEncode(returnUrl)}");

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
            $"/update-email/confirmation?email={UrlEncode(protectedEmail.EncryptedValue)}&returnUrl={UrlEncode(returnUrl)}");

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

        var newEmail = Faker.Internet.Email();
        var emailVerificationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var pinResult = await emailVerificationService.GeneratePin(newEmail);
        Assert.True(pinResult.Succeeded);

        var returnUrl = "/_tests/empty";

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/update-email/confirmation?returnUrl={UrlEncode(returnUrl)}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pinResult.Pin! }
            }
        };

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

        var newEmail = Faker.Internet.Email();

        // The real PIN generation service never generates pins that start with a '0'
        var pin = "01234";

        var protectedEmail = HostFixture.Services.GetRequiredService<ProtectedStringFactory>().CreateFromPlainValue(newEmail);

        var returnUrl = "/_tests/empty";

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/update-email/confirmation?email={UrlEncode(protectedEmail.EncryptedValue)}&returnUrl={UrlEncode(returnUrl)}")
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

        var newEmail = Faker.Internet.Email();
        var pin = "0";

        var protectedEmail = HostFixture.Services.GetRequiredService<ProtectedStringFactory>().CreateFromPlainValue(newEmail);

        var returnUrl = "/_tests/empty";

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/update-email/confirmation?email={UrlEncode(protectedEmail.EncryptedValue)}&returnUrl={UrlEncode(returnUrl)}")
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

        var newEmail = Faker.Internet.Email();
        var pin = "0123345678";

        var protectedEmail = HostFixture.Services.GetRequiredService<ProtectedStringFactory>().CreateFromPlainValue(newEmail);

        var returnUrl = "/_tests/empty";

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/update-email/confirmation?email={UrlEncode(protectedEmail.EncryptedValue)}&returnUrl={UrlEncode(returnUrl)}")
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

        var newEmail = Faker.Internet.Email();
        var pin = "abc";

        var protectedEmail = HostFixture.Services.GetRequiredService<ProtectedStringFactory>().CreateFromPlainValue(newEmail);

        var returnUrl = "/_tests/empty";

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/update-email/confirmation?email={UrlEncode(protectedEmail.EncryptedValue)}&returnUrl={UrlEncode(returnUrl)}")
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

        var newEmail = Faker.Internet.Email();
        var emailVerificationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var pinResult = await emailVerificationService.GeneratePin(newEmail);
        Assert.True(pinResult.Succeeded);
        Clock.AdvanceBy(TimeSpan.FromHours(1));
        Spy.Get(emailVerificationService).Reset();

        var protectedEmail = HostFixture.Services.GetRequiredService<ProtectedStringFactory>().CreateFromPlainValue(newEmail);

        var returnUrl = "/_tests/empty";

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/update-email/confirmation?email={UrlEncode(protectedEmail.EncryptedValue)}&returnUrl={UrlEncode(returnUrl)}")
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

        HostFixture.EmailVerificationService.Verify(mock => mock.GeneratePin(newEmail), Times.Once);
    }

    [Fact]
    public async Task Post_PinExpiredMoreThanTwoHoursAgo_ReturnsErrorAndDoesNotSendANewPin()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var newEmail = Faker.Internet.Email();
        var emailVerificationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var emailVerificationOptions = HostFixture.Services.GetRequiredService<IOptions<EmailVerificationOptions>>();
        var pinResult = await emailVerificationService.GeneratePin(newEmail);
        Assert.True(pinResult.Succeeded);
        Clock.AdvanceBy(TimeSpan.FromHours(2) + TimeSpan.FromSeconds(emailVerificationOptions.Value.PinLifetimeSeconds));
        Spy.Get(emailVerificationService).Reset();

        var protectedEmail = HostFixture.Services.GetRequiredService<ProtectedStringFactory>().CreateFromPlainValue(newEmail);

        var returnUrl = "/_tests/empty";

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/update-email/confirmation?email={UrlEncode(protectedEmail.EncryptedValue)}&returnUrl={UrlEncode(returnUrl)}")
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

        HostFixture.EmailVerificationService.Verify(mock => mock.GeneratePin(newEmail), Times.Never);
    }

    [Fact]
    public async Task Post_ValidPin_UpdatesEmailEmitsEventAndRedirects()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var newEmail = Faker.Internet.Email();
        var emailVerificationService = HostFixture.Services.GetRequiredService<IEmailVerificationService>();
        var pinResult = await emailVerificationService.GeneratePin(newEmail);
        Assert.True(pinResult.Succeeded);

        var protectedEmail = HostFixture.Services.GetRequiredService<ProtectedStringFactory>().CreateFromPlainValue(newEmail);

        var returnUrl = "/_tests/empty";

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"/update-email/confirmation?email={UrlEncode(protectedEmail.EncryptedValue)}&returnUrl={UrlEncode(returnUrl)}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Code", pinResult.Pin! }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(returnUrl, response.Headers.Location?.OriginalString);

        user = await TestData.WithDbContext(dbContext => dbContext.Users.SingleAsync(u => u.UserId == user.UserId));
        Assert.Equal(newEmail, user.EmailAddress);
        Assert.Equal(Clock.UtcNow, user.Updated);

        EventObserver.AssertEventsSaved(
            e =>
            {
                var userUpdatedEvent = Assert.IsType<UserUpdatedEvent>(e);
                Assert.Equal(Clock.UtcNow, userUpdatedEvent.CreatedUtc);
                Assert.Equal(UserUpdatedEventSource.ChangedByUser, userUpdatedEvent.Source);
                Assert.Equal(UserUpdatedEventChanges.EmailAddress, userUpdatedEvent.Changes);
                Assert.Equal(user.UserId, userUpdatedEvent.User.UserId);
            });

        var redirectedResponse = await response.FollowRedirect(HttpClient);
        var redirectedDoc = await redirectedResponse.GetDocument();
        AssertEx.HtmlDocumentHasFlashSuccess(redirectedDoc, "Email address updated");
    }

    private static string UrlEncode(string value) => UrlEncoder.Default.Encode(value);
}
