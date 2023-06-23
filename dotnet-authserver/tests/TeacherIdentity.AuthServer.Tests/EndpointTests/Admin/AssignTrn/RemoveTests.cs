using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Events;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin.AssignTrn;

public class RemoveTests : TestBase
{
    public RemoveTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_UnauthenticatedUser_RedirectsToSignIn()
    {
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: true);

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, $"/admin/users/{user.UserId}/remove-trn");
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: true);

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Get, $"/admin/users/{user.UserId}/remove-trn");
    }

    [Fact]
    public async Task Get_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{userId}/remove-trn");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_UserDoesNotHaveTrnAssigned_ReturnsBadRequest()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: false);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}/remove-trn");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsOk()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: true);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}/remove-trn");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_UnauthenticatedUser_RedirectsToSignIn()
    {
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: true);

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Post, $"/admin/users/{user.UserId}/remove-trn");
    }

    [Fact]
    public async Task Post_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: true);

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Post, $"/admin/users/{user.UserId}/remove-trn");
    }

    [Fact]
    public async Task Post_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{userId}/remove-trn")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "ConfirmRemoveTrn", bool.TrueString }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_UserDoesNotHaveTrnAssigned_ReturnsBadRequest()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: false);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/remove-trn")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "ConfirmRemoveTrn", bool.TrueString }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_ConfirmNotChecked_ReturnsErrors()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: true);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/remove-trn")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "ConfirmRemoveTrn", "Confirm you want to remove the TRN");
    }

    [Fact]
    public async Task Post_ValidRequest_RemovesTrnCreatesEventAndRedirects()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: Models.UserType.Default, hasTrn: true);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/remove-trn")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "ConfirmRemoveTrn", bool.TrueString }
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
            Assert.Null(user.Trn);
            Assert.Null(user.TrnAssociationSource);
            Assert.Null(user.TrnLookupStatus);
            Assert.Equal(Clock.UtcNow, user.Updated);
        });

        EventObserver.AssertEventsSaved(
            e =>
            {
                var userUpdatedEvent = Assert.IsType<UserUpdatedEvent>(e);
                Assert.Equal(Clock.UtcNow, userUpdatedEvent.CreatedUtc);
                Assert.Equal(UserUpdatedEventSource.SupportUi, userUpdatedEvent.Source);
                Assert.Equal(UserUpdatedEventChanges.Trn | UserUpdatedEventChanges.TrnLookupStatus, userUpdatedEvent.Changes);
                Assert.Equal(user.UserId, userUpdatedEvent.User.UserId);
                Assert.Null(user.Trn);
                Assert.Null(user.TrnLookupStatus);
            });
    }
}
