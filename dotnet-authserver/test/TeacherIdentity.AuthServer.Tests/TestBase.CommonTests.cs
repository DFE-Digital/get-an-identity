using Flurl;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Tests;

public partial class TestBase
{
    public async Task InvalidAuthenticationState_ReturnsBadRequest(HttpMethod method, string url, HttpContent? content = null)
    {
        // Arrange
        var fullUrl = new Url(url).RemoveQueryParam(AuthenticationStateMiddleware.IdQueryParameterName);
        var request = new HttpRequestMessage(method, fullUrl);

        if (content is not null)
        {
            request.Content = content;
        }

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }
}
