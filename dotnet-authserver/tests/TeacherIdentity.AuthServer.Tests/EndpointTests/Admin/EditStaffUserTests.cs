using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin;

public class EditStaffUserTests : TestBase
{
    public EditStaffUserTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_UnauthenticatedUser_RedirectsToSignIn()
    {
        var user = await TestData.CreateUser(userType: UserType.Staff);

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, $"/admin/staff/{user.UserId}");
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: UserType.Staff);

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Get, $"/admin/staff/{user.UserId}");
    }

    [Fact]
    public async Task Get_UserIsSelf_ReturnsForbidden()
    {
        // Arrange
        var userId = TestUsers.AdminUserWithAllRoles.UserId;
        HostFixture.SetUserId(userId);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/staff/{userId}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Staff);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/staff/{user.UserId}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_UnauthenticatedUser_RedirectsToSignIn()
    {
        var user = await TestData.CreateUser(userType: UserType.Staff);

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Post, $"/admin/staff/{user.UserId}");
    }

    [Fact]
    public async Task Post_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: UserType.Staff);

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Post, $"/admin/staff/{user.UserId}");
    }

    [Fact]
    public async Task Post_UserIsSelf_ReturnsForbidden()
    {
        // Arrange
        var userId = TestUsers.AdminUserWithAllRoles.UserId;
        HostFixture.SetUserId(userId);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/staff/{userId}");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_WithEmailAddressAlreadyInUse_RendersError()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Staff);

        string newEmail = TestUsers.AdminUserWithAllRoles.EmailAddress;
        string newFirstName = Faker.Name.First();
        string newLastName = Faker.Name.Last();
        var newRole1 = StaffRoles.GetAnIdentityAdmin;
        var newRole2 = StaffRoles.GetAnIdentitySupport;

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/staff/{user.UserId}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", newEmail },
                { "FirstName", newFirstName },
                { "LastName", newLastName },
                { "StaffRoles", newRole1 },
                { "StaffRoles", newRole2 }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Email", "Another user has the specified email address");
    }

    [Fact]
    public async Task Post_ValidRequest_CreatesUserEmitsEventAndRedirectsToStaffPage()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Staff);

        string newEmail = Faker.Internet.Email();
        string newFirstName = Faker.Name.First();
        string newLastName = Faker.Name.Last();
        var newRole1 = StaffRoles.GetAnIdentityAdmin;
        var newRole2 = StaffRoles.GetAnIdentitySupport;

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/staff/{user.UserId}")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Email", newEmail },
                { "FirstName", newFirstName },
                { "LastName", newLastName },
                { "StaffRoles", newRole1 },
                { "StaffRoles", newRole2 }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal("/admin/staff", response.Headers.Location?.OriginalString);

        var expectedRoles = new[] { newRole1, newRole2 };

        var userId = await TestData.WithDbContext(async dbContext =>
        {
            user = await dbContext.Users.SingleOrDefaultAsync(u => u.UserId == user.UserId);

            Assert.NotNull(user);
            Assert.Equal(newFirstName, user!.FirstName);
            Assert.Equal(newLastName, user!.LastName);
            Assert.Equal(expectedRoles, user!.StaffRoles);
            Assert.Equal(Clock.UtcNow, user!.Created);
            Assert.Equal(Clock.UtcNow, user!.Updated);

            return user.UserId;
        });

        EventObserver.AssertEventsSaved(
            e =>
            {
                var staffUserAdded = Assert.IsType<StaffUserUpdatedEvent>(e);
                Assert.Equal(TestUsers.AdminUserWithAllRoles.UserId, staffUserAdded.UpdatedByUserId);
                Assert.Equal(Clock.UtcNow, staffUserAdded.CreatedUtc);
                Assert.Equal(userId, staffUserAdded.User.UserId);
            });

        var redirectedResponse = await response.FollowRedirect(HttpClient);
        var redirectedDoc = await redirectedResponse.GetDocument();
        AssertEx.HtmlDocumentHasFlashSuccess(redirectedDoc, "Staff user updated");
    }
}
