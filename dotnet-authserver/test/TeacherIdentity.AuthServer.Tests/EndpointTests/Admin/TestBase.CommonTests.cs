namespace TeacherIdentity.AuthServer.Tests.EndpointTests.Admin;

public partial class TestBase
{
    public async Task AuthenticatedUserDoesNotHavePermission_ReturnsForbidden(HttpMethod method, string url, HttpContent? content = null)
    {
        // Arrange
        var request = new HttpRequestMessage(method, url);

        // Act
        var response = await AuthenticatedHttpClientWithNoRoles!.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, (int)response.StatusCode);
    }

    public async Task UnauthenticatedUser_RedirectsToSignIn(HttpMethod method, string url, HttpContent? content = null)
    {
        // Arrange
        var httpClient = CreateAuthenticatedHttpClient();

        var request = new HttpRequestMessage(method, url);

        if (content is not null)
        {
            request.Content = content;
        }

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.StartsWith("/sign-in/email", response.Headers.Location?.OriginalString);
    }
}
