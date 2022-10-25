namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin;

public partial class TestBase
{
    public async Task AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod method, string url, HttpContent? content = null)
    {
        // Arrange
        HostFixture.SetUserId(TestUsers.AdminUserWithNoRoles.UserId);

        var request = new HttpRequestMessage(method, url);

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, (int)response.StatusCode);
    }

    public async Task UnauthenticatedUser_RedirectsToSignIn(HttpMethod method, string url, HttpContent? content = null)
    {
        // Arrange
        HostFixture.SetUserId(null);

        var request = new HttpRequestMessage(method, url);

        if (content is not null)
        {
            request.Content = content;
        }

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/email", response.Headers.Location?.OriginalString);
    }
}
