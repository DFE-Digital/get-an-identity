using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin.MergeUser;

public class SelectUserTests : TestBase
{
    public SelectUserTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_UnauthenticatedUser_RedirectsToSignIn()
    {
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, $"/admin/users/{user.UserId}/merge");
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Get, $"/admin/users/{user.UserId}/merge");
    }

    [Fact]
    public async Task Get_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{userId}/merge");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsOk()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}/merge");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_UnauthenticatedUser_RedirectsToSignIn()
    {
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Post, $"/admin/users/{user.UserId}/merge");
    }

    [Fact]
    public async Task Post_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Post, $"/admin/users/{user.UserId}/merge");
    }

    [Fact]
    public async Task Post_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{userId}/merge");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_EmptyUserIdToMerge_ReturnsError()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/merge")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "UserIdToMerge", "Select which user you want to merge");
    }

    [Fact]
    public async Task Post_InvalidUserIdToMerge_ReturnsError()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var invalidUserToMerge = await TestData.CreateUser(userType: UserType.Staff, hasTrn: false);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/merge")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "UserIdToMerge", invalidUserToMerge.UserId }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "UserIdToMerge", "You must select a user ID from the given list");
    }

    [Fact]
    public async Task Post_ValidForm_RedirectsToConfirm()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);
        var userToMerge = await TestData.CreateUser(userType: UserType.Default, hasTrn: false);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/merge")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "UserIdToMerge", userToMerge.UserId }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith($"/admin/users/{user.UserId}/merge/{userToMerge.UserId}/confirm", response.Headers.Location?.OriginalString);
    }
}
