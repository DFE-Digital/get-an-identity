using Flurl;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin;

public class EditUserMobileNumberTests : TestBase
{
    public EditUserMobileNumberTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_UnauthenticatedUser_RedirectsToSignIn()
    {
        var user = await TestData.CreateUser(userType: Models.UserType.Default);

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, UrlSegmentPath(user.UserId));
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: Models.UserType.Default);

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Get, UrlSegmentPath(user.UserId));
    }

    [Fact]
    public async Task Get_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, UrlSegmentPath(userId));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_UserIsNotDefaultType_ReturnsNotFound()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Staff);
        var request = new HttpRequestMessage(HttpMethod.Get, UrlSegmentPath(user.UserId));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default);
        var request = new HttpRequestMessage(HttpMethod.Get, UrlSegmentPath(user.UserId));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_UnauthenticatedUser_RedirectsToSignIn()
    {
        var user = await TestData.CreateUser(userType: Models.UserType.Default);

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Post, UrlSegmentPath(user.UserId));
    }

    [Fact]
    public async Task Post_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: Models.UserType.Default);

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Post, UrlSegmentPath(user.UserId));
    }

    [Fact]
    public async Task Post_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Post, UrlSegmentPath(userId));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_UserIsNotDefaultType_ReturnsNotFound()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Staff);
        var request = new HttpRequestMessage(HttpMethod.Post, UrlSegmentPath(user.UserId));

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(InvalidMobileNumberData))]
    public async Task Post_InvalidPhoneNumber_RendersError(string mobileNumber, string expectedErrorMessage)
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default);
        var request = new HttpRequestMessage(HttpMethod.Post, UrlSegmentPath(user.UserId))
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "MobileNumber", mobileNumber }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "MobileNumber", expectedErrorMessage);
    }

    [Fact]
    public async Task Post_PhoneNumberAlreadyInUse_RendersError()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasMobileNumber: true);
        var otherUser = await TestData.CreateUser(hasMobileNumber: true);

        var request = new HttpRequestMessage(HttpMethod.Post, UrlSegmentPath(user.UserId))
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "MobileNumber", otherUser.MobileNumber! }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "MobileNumber", "This phone number is already in use - Enter a different phone number.");
    }

    [Theory]
    [InlineData(false, false, UserUpdatedEventChanges.None)]
    [InlineData(true, true, UserUpdatedEventChanges.MobileNumber)]
    public async Task Post_ValidRequest_SetsUserNameEmitsEventAndRedirects(
        bool changedPhoneNumber,
        bool expectEvent,
        UserUpdatedEventChanges expectedChanges)
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasMobileNumber: true);

        var newPhoneNumber = changedPhoneNumber ? TestData.GenerateUniqueMobileNumber() : user.MobileNumber!;

        var request = new HttpRequestMessage(HttpMethod.Post, UrlSegmentPath(user.UserId))
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "MobileNumber", newPhoneNumber }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(UrlSegmentPath(user.UserId).RemovePathSegment(), response.Headers.Location?.OriginalString);

        await TestData.WithDbContext(async dbContext =>
        {
            user = await dbContext.Users.SingleAsync(u => u.UserId == user.UserId);
            Assert.Equal(newPhoneNumber, user.MobileNumber);
        });

        if (expectEvent)
        {
            EventObserver.AssertEventsSaved(
                e =>
                {
                    var userUpdatedEvent = Assert.IsType<UserUpdatedEvent>(e);
                    Assert.Equal(Clock.UtcNow, userUpdatedEvent.CreatedUtc);
                    Assert.Equal(UserUpdatedEventSource.SupportUi, userUpdatedEvent.Source);
                    Assert.Equal(expectedChanges, userUpdatedEvent.Changes);
                    Assert.Equal(user.UserId, userUpdatedEvent.User.UserId);
                    Assert.Equal(TestUsers.AdminUserWithAllRoles.UserId, userUpdatedEvent.UpdatedByUserId);
                });
        }
        else
        {
            EventObserver.AssertEventsSaved();
        }
    }

    public static TheoryData<string, string> InvalidMobileNumberData { get; } = new()
    {
        {"07", "Enter a valid phone number"},
    };

    private static string UrlSegmentPath(Guid userId) => $"/admin/users/{userId}/phone";
}
