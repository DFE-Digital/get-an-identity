using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin;

public class EditUserEmailTests : TestBase
{
    public EditUserEmailTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_UnauthenticatedUser_RedirectsToSignIn()
    {
        var user = await TestData.CreateUser(userType: Models.UserType.Default);

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, $"/admin/users/{user.UserId}/email");
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: Models.UserType.Default);

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Get, $"/admin/users/{user.UserId}/email");
    }

    [Fact]
    public async Task Get_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{userId}/email");

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
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}/email");

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
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}/email");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_UnauthenticatedUser_RedirectsToSignIn()
    {
        var user = await TestData.CreateUser(userType: Models.UserType.Default);

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Post, $"/admin/users/{user.UserId}/email");
    }

    [Fact]
    public async Task Post_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: Models.UserType.Default);

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Post, $"/admin/users/{user.UserId}/email");
    }

    [Fact]
    public async Task Post_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{userId}/email");

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
        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/email");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(InvalidEmailData))]
    public async Task Post_InvalidEmail_RendersError(string email, string expectedErrorMessage)
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/email")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", email }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Email", expectedErrorMessage);
    }

    [Fact]
    public async Task Post_EmailAlreadyInUse_RendersError()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default);
        var otherUser = await TestData.CreateUser();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/email")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", otherUser.EmailAddress }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Email", "This email address is already in use - Enter a different email address");
    }

    [Theory]
    [InlineData(false, false, UserUpdatedEventChanges.None)]
    [InlineData(true, true, UserUpdatedEventChanges.EmailAddress)]
    public async Task Post_ValidRequest_SetsUserNameEmitsEventAndRedirects(
        bool changeEmail,
        bool expectEvent,
        UserUpdatedEventChanges expectedChanges)
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default);

        var newEmail = changeEmail ? Faker.Internet.Email() : user.EmailAddress;

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/email")
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
        Assert.Equal($"/admin/users/{user.UserId}", response.Headers.Location?.OriginalString);

        await TestData.WithDbContext(async dbContext =>
        {
            user = await dbContext.Users.SingleAsync(u => u.UserId == user.UserId);
            Assert.Equal(newEmail, user.EmailAddress);
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

    public static TheoryData<string, string> InvalidEmailData { get; } = new()
    {
        { "", "Enter an email address" },
        { "xxx", "Enter a valid email address" },
    };
}
