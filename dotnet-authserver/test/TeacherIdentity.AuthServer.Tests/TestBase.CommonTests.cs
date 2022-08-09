using Flurl;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Tests;

public partial class TestBase
{
    public async Task InvalidAuthenticationState_ReturnsBadRequest(HttpMethod method, string url)
    {
        // Arrange
        var fullUrl = new Url(url).RemoveQueryParam(AuthenticationStateMiddleware.IdQueryParameterName);
        var request = new HttpRequestMessage(method, fullUrl);

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);
    }
}
