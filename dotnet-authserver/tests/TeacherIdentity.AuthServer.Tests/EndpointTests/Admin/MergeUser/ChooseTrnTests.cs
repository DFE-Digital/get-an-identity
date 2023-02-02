using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin.MergeUser;

public class ChooseTrnTests : TestBase
{
    public ChooseTrnTests(HostFixture hostFixture)
        : base(hostFixture)
    {
    }

    [Fact]
    public async Task Get_UnauthenticatedUser_RedirectsToSignIn()
    {
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);
        var userToMerge = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Get, $"/admin/users/{user.UserId}/merge/{userToMerge.UserId}/choose-trn");
    }

    [Fact]
    public async Task Get_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);
        var userToMerge = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Get, $"/admin/users/{user.UserId}/merge/{userToMerge.UserId}/choose-trn");
    }

    [Fact]
    public async Task Get_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userToMerge = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{userId}/merge/{userToMerge.UserId}/choose-trn");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_UserToMergeDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);
        var userIdToMerge = Guid.NewGuid();

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}/merge/{userIdToMerge}/choose-trn");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Get_ValidRequest_ReturnsOk()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);
        var userToMerge = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);

        var request = new HttpRequestMessage(HttpMethod.Get, $"/admin/users/{user.UserId}/merge/{userToMerge.UserId}/choose-trn");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status200OK, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_UnauthenticatedUser_RedirectsToSignIn()
    {
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);
        var userToMerge = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);

        await UnauthenticatedUser_RedirectsToSignIn(HttpMethod.Post, $"/admin/users/{user.UserId}/merge/{userToMerge.UserId}/choose-trn");
    }

    [Fact]
    public async Task Post_AuthenticatedUserDoesNotHavePermission_ReturnsForbidden()
    {
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);
        var userToMerge = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);

        await AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod.Post, $"/admin/users/{user.UserId}/merge/{userToMerge.UserId}/choose-trn");
    }

    [Fact]
    public async Task Post_UserDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userToMerge = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{userId}/merge/{userToMerge.UserId}/choose-trn");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_UserToMergeDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);
        var userIdToMerge = Guid.NewGuid();

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/merge/{userIdToMerge}/choose-trn");

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status404NotFound, (int)response.StatusCode);
    }

    [Fact]
    public async Task Post_NullChosenTrn_ReturnsError()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);
        var userToMerge = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/merge/{userToMerge.UserId}/choose-trn")
        {
            Content = new FormUrlEncodedContentBuilder()
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        await AssertEx.HtmlResponseHasError(response, "Trn", "Select the TRN you want to keep");
    }

    [Fact]
    public async Task Post_ValidForm_RedirectsToConfirm()
    {
        // Arrange
        var user = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);
        var userToMerge = await TestData.CreateUser(userType: UserType.Default, hasTrn: true);

        var request = new HttpRequestMessage(HttpMethod.Post, $"/admin/users/{user.UserId}/merge/{userToMerge.UserId}/choose-trn")
        {
            Content = new FormUrlEncodedContentBuilder()
            {
                { "Trn", userToMerge.Trn! }
            }
        };

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith($"/admin/users/{user.UserId}/merge/{userToMerge.UserId}/confirm", response.Headers.Location?.OriginalString);
    }
}
