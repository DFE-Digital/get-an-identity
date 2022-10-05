using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin;

public class AddStaffUserTests : TestBase
{
    public AddStaffUserTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_UnauthenticatedUser_RedirectsToSignIn()
    {
        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, "/admin/staff/new");
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/staff/new");

        // Act
        var response = await AuthenticatedHttpClientWithNoRoles!.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_RendersExpectedContent()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/admin/staff/new");

        // Act
        var response = await AuthenticatedHttpClient!.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_UnauthenticatedUser_RedirectsToSignIn()
    {
        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Post, "/admin/staff/new");
    }

    [Fact]
    public async Task Post_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/staff/new");

        // Act
        var response = await AuthenticatedHttpClientWithNoRoles!.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_WithEmailAddressAlreadyInUse_RendersError()
    {
        // Arrange
        string email = TestUsers.AdminUserWithAllRoles.EmailAddress;
        string firstName = Faker.Name.First();
        string lastName = Faker.Name.Last();
        var role1 = StaffRoles.GetAnIdentityAdmin;
        var role2 = StaffRoles.GetAnIdentitySupport;

        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/staff/new")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Email", email)
                .Add("FirstName", firstName)
                .Add("LastName", lastName)
                .Add("StaffRoles", role1)
                .Add("StaffRoles", role2)
                .ToContent()
        };

        // Act
        var response = await AuthenticatedHttpClient!.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Email", "A user already exists with the specified email address");
    }

    [Fact]
    public async Task Post_ValidRequest_CreatesUserEmitsEventAndRedirectsToStaffPage()
    {
        // Arrange
        string email = Faker.Internet.Email();
        string firstName = Faker.Name.First();
        string lastName = Faker.Name.Last();
        var role1 = StaffRoles.GetAnIdentityAdmin;
        var role2 = StaffRoles.GetAnIdentitySupport;

        var request = new HttpRequestMessage(HttpMethod.Post, "/admin/staff/new")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("Email", email)
                .Add("FirstName", firstName)
                .Add("LastName", lastName)
                .Add("StaffRoles", role1)
                .Add("StaffRoles", role2)
                .ToContent()
        };

        // Act
        var response = await AuthenticatedHttpClient!.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal("/admin/staff", response.Headers.Location?.OriginalString);

        var expectedRoles = new[] { role1, role2 };

        var userId = await TestData.WithDbContext(async dbContext =>
        {
            var user = await dbContext.Users.SingleOrDefaultAsync(u => u.EmailAddress == email);

            Assert.NotNull(user);
            Assert.Equal(firstName, user!.FirstName);
            Assert.Equal(lastName, user!.LastName);
            Assert.Equal(expectedRoles, user!.StaffRoles);
            Assert.Equal(Clock.UtcNow, user!.Created);
            Assert.Equal(Clock.UtcNow, user!.Updated);

            return user.UserId;
        });

        EventObserver.AssertEventsSaved(
            e =>
            {
                var staffUserAdded = Assert.IsType<StaffUserAdded>(e);
                Assert.Equal(TestUsers.AdminUserWithAllRoles.UserId, staffUserAdded.AddedByUserId);
                Assert.Equal(Clock.UtcNow, staffUserAdded.CreatedUtc);
                Assert.Equal(userId, staffUserAdded.UserId);
                Assert.Equal(email, staffUserAdded.EmailAddress);
                Assert.Equal(firstName, staffUserAdded.FirstName);
                Assert.Equal(lastName, staffUserAdded.LastName);
                Assert.Equal(expectedRoles, staffUserAdded.StaffRoles);
            });
    }
}
