using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin.MergeUser;

public class ConfirmTests : TestBase
{
    public ConfirmTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_UnauthenticatedUser_RedirectsToSignIn()
    {
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var userToMerge = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, $"/admin/users/{user.UserId}/merge/{userToMerge.UserId}/confirm");
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var userToMerge = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Get, $"/admin/users/{user.UserId}/merge/{userToMerge.UserId}/confirm");
    }

    [Fact]
    public async Task Get_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userToMerge = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{userId}/merge/{userToMerge.UserId}/confirm");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_UserToMergeDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var userIdToMerge = Guid.NewGuid();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}/merge/{userIdToMerge}/confirm");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequestForUsersWithoutTrn_RendersExpectedContent()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var userToMerge = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}/merge/{userToMerge.UserId}/confirm");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal(user.EmailAddress, doc.GetSummaryListValueForKey("Email address"));
        Assert.Equal($"{user.FirstName} {user.LastName}", doc.GetSummaryListValueForKey("Name"));
        Assert.Equal("None", doc.GetSummaryListValueForKey("TRN"));
        Assert.Empty(doc.GetSummaryListActionsForKey("TRN"));
    }

    [Fact]
    public async Task Get_ValidRequestUsersHaveDifferentTrns_RendersUserTrnAndChangeTrnAction()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);
        var userToMerge = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}/merge/{userToMerge.UserId}/confirm");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.Equal(user.Trn, doc.GetSummaryListValueForKey("TRN"));
        Assert.NotEmpty(doc.GetSummaryListActionsForKey("TRN"));
    }

    [Fact]
    public async Task Post_UnauthenticatedUser_RedirectsToSignIn()
    {
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var userToMerge = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Post, $"/admin/users/{user.UserId}/merge/{userToMerge.UserId}/confirm");
    }

    [Fact]
    public async Task Post_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var userToMerge = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Post, $"/admin/users/{user.UserId}/merge/{userToMerge.UserId}/confirm");
    }

    [Fact]
    public async Task Post_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userToMerge = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{userId}/merge/{userToMerge.UserId}/confirm");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_UserToMergeDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var userIdToMerge = Guid.NewGuid();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/merge/{userIdToMerge}/confirm");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_ValidForm_MergesUserToMergeRedirectsToUsers()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var userToMerge = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/merge/{userToMerge.UserId}/confirm")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith($"/admin/users", response.Headers.Location?.OriginalString);

        await TestData.WithDbContext(async dbContext =>
        {
            var mergedUser = await dbContext.Users.IgnoreQueryFilters().Where(u => u.UserId == userToMerge.UserId).SingleOrDefaultAsync();
            Assert.NotNull(mergedUser);
            Assert.True(mergedUser.IsDeleted);
            Assert.Equal(mergedUser.MergedWithUserId, user.UserId);
        });

        EventObserver.AssertEventsSaved(
            e =>
            {
                var userMergedEvent = Assert.IsType<UserMergedEvent>(e);
                Assert.Equal(userToMerge, userMergedEvent.User);
                Assert.Equal(user.UserId, userMergedEvent.MergedWithUserId);
                Assert.Equal(Clock.UtcNow, userMergedEvent.CreatedUtc);
            });
    }
}
