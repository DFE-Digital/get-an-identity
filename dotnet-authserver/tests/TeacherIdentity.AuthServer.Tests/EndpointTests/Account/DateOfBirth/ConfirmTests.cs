using System.Text.Encodings.Web;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;
using User = TeacherIdentity.AuthServer.Models.User;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Account.DateOfBirth;

public class ConfirmTests : TestBase
{
    public ConfirmTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_NoDateOfBirth_ReturnsBadRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, $"/account/date-of-birth/confirm");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_InvalidDateOfBirth_ReturnsBadRequest()
    {
        // Arrange
        var dateOfBirthString = "";
        var protectedDateOfBirthString = HostFixture.Services.GetRequiredService<ProtectedStringFactory>().CreateFromPlainValue(dateOfBirthString);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/account/date-of-birth/confirm?dateOfBirth={UrlEncode(protectedDateOfBirthString.EncryptedValue)}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsSuccess()
    {
        var dateOfBirth = new DateOnly(2000, 1, 1);

        var protectedDateOfBirth = HostFixture.Services.GetRequiredService<ProtectedStringFactory>().CreateFromPlainValue(dateOfBirth.ToString());

        var request = new HttpRequestMessage(HttpMethod.Get, $"/account/date-of-birth/confirm?dateOfBirth={UrlEncode(protectedDateOfBirth.EncryptedValue)}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task Post_DateOfBirthChangeDisabled_ReturnsBadRequest(bool hasDobConflict, bool hasPendingDobChange)
    {
        // Arrange
        HostFixture.SetUserId(TestUsers.DefaultUserWithTrn.UserId);
        MockDqtApiResponse(TestUsers.DefaultUserWithTrn, hasDobConflict, hasPendingDobChange);

        var dateOfBirth = new DateOnly(2000, 1, 1);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/date-of-birth")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "DateOfBirth.Day", dateOfBirth.Day.ToString() },
                { "DateOfBirth.Month", dateOfBirth.Month.ToString() },
                { "DateOfBirth.Year", dateOfBirth.Year.ToString() },
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_NoDateOfBirth_ReturnsBadRequest()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/date-of-birth/confirm");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidForm_UpdatesDateOfBirthEmitsEventAndRedirects()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default);
        HostFixture.SetUserId(user.UserId);

        var client = TestClients.Client1;
        var redirectUri = client.RedirectUris.First().GetLeftPart(UriPartial.Authority);

        var returnUrl = $"/account?client_id={client.ClientId}&redirect_uri={Uri.EscapeDataString(redirectUri)}";

        var newDateOfBirth = new DateOnly(2000, 1, 1);
        var protectedDateOfBirth = HostFixture.Services.GetRequiredService<ProtectedStringFactory>().CreateFromPlainValue(newDateOfBirth.ToString());

        var request = new HttpRequestMessage(HttpMethod.Post, $"/account/date-of-birth/confirm?dateOfBirth={UrlEncode(protectedDateOfBirth.EncryptedValue)}&returnUrl={UrlEncode(returnUrl)}")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(returnUrl, response.Headers.Location?.OriginalString);

        user = await TestData.WithDbContext(dbContext => dbContext.Users.SingleAsync(u => u.UserId == user.UserId));
        Assert.Equal(newDateOfBirth, user.DateOfBirth);
        Assert.Equal(Clock.UtcNow, user.Updated);

        EventObserver.AssertEventsSaved(
            e =>
            {
                var userUpdatedEvent = Assert.IsType<UserUpdatedEvent>(e);
                Assert.Equal(Clock.UtcNow, userUpdatedEvent.CreatedUtc);
                Assert.Equal(UserUpdatedEventSource.ChangedByUser, userUpdatedEvent.Source);
                Assert.Equal(UserUpdatedEventChanges.DateOfBirth, userUpdatedEvent.Changes);
                Assert.Equal(user.UserId, userUpdatedEvent.User.UserId);
            });

        var redirectedResponse = await response.FollowRedirect(HttpClient);
        var redirectedDoc = await redirectedResponse.GetDocument();
        AssertEx.HtmlDocumentHasFlashSuccess(redirectedDoc, "Your date of birth has been updated");
    }

    private static string UrlEncode(string value) => UrlEncoder.Default.Encode(value);

    private void MockDqtApiResponse(User user, bool hasDobConflict, bool hasPendingDobChange)
    {
        HostFixture.DqtApiClient.Setup(mock => mock.GetTeacherByTrn(user.Trn!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AuthServer.Services.DqtApi.TeacherInfo()
            {
                DateOfBirth = hasDobConflict ? user.DateOfBirth!.Value.AddDays(1) : user.DateOfBirth!.Value,
                FirstName = user.FirstName,
                LastName = user.LastName,
                NationalInsuranceNumber = Faker.Identification.UkNationalInsuranceNumber(),
                Trn = user.Trn!,
                PendingDateOfBirthChange = hasPendingDobChange
            });
    }
}
