using Flurl;
using TeacherIdentity.AuthServer.State;
using static TeacherIdentity.AuthServer.Tests.AuthenticationStateHelper;

namespace TeacherIdentity.AuthServer.Tests;

public partial class TestBase
{
    public async Task InvalidAuthenticationState_ReturnsBadRequest(HttpMethod method, string url, HttpContent? content = null)
    {
        // Arrange
        var fullUrl = new Url(url).SetQueryParam(AuthenticationStateMiddleware.IdQueryParameterName, "xxx");
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

    public async Task MissingAuthenticationState_ReturnsBadRequest(HttpMethod method, string url, HttpContent? content = null)
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

    public async Task JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(HttpMethod method, string url, HttpContent? content = null)
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true);
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Completed(user, firstTimeSignIn: true));

        var fullUrl = new Url(url).SetQueryParam(AuthenticationStateMiddleware.IdQueryParameterName, authStateHelper.AuthenticationState.JourneyId);
        var request = new HttpRequestMessage(method, fullUrl);

        if (content is not null)
        {
            request.Content = content;
        }

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status302Found, (int)response.StatusCode);
        Assert.Equal(authStateHelper.AuthenticationState.PostSignInUrl, response.Headers.Location?.OriginalString);
    }

    public async Task JourneyIsAlreadyCompleted_DoesNotRedirectToPostSignInUrl(HttpMethod method, string url, HttpContent? content = null)
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true);
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Completed(user, firstTimeSignIn: true));

        var fullUrl = new Url(url).SetQueryParam(AuthenticationStateMiddleware.IdQueryParameterName, authStateHelper.AuthenticationState.JourneyId);
        var request = new HttpRequestMessage(method, fullUrl);

        if (content is not null)
        {
            request.Content = content;
        }

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.NotEqual(authStateHelper.AuthenticationState.PostSignInUrl, response.Headers.Location?.OriginalString);
    }

    public async Task JourneyHasExpired_RendersErrorPage(
        Func<Configure, Func<AuthenticationState, Task>> configureAuthenticationHelper,
        HttpMethod method,
        string url,
        HttpContent? content = null)
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true);
        var authStateHelper = await CreateAuthenticationStateHelper(c => async s =>
        {
            s.Reset(Clock.UtcNow.Subtract(TimeSpan.FromMinutes(20)));
            await configureAuthenticationHelper(c)(s);
        });

        var fullUrl = new Url(url).SetQueryParam(AuthenticationStateMiddleware.IdQueryParameterName, authStateHelper.AuthenticationState.JourneyId);
        var request = new HttpRequestMessage(method, fullUrl);

        if (content is not null)
        {
            request.Content = content;
        }

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status400BadRequest, (int)response.StatusCode);

        var doc = await response.GetDocument();
        Assert.NotNull(doc.GetElementByTestId("JourneyExpiredError"));
    }

    public async Task JourneyHasExpired_DoesNotRenderErrorPage(
        Func<Configure, Func<AuthenticationState, Task>> configureAuthenticationHelper,
        HttpMethod method,
        string url,
        HttpContent? content = null)
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true);
        var authStateHelper = await CreateAuthenticationStateHelper(c => async s =>
        {
            s.Reset(Clock.UtcNow.Subtract(TimeSpan.FromMinutes(20)));
            await configureAuthenticationHelper(c)(s);
        });

        var fullUrl = new Url(url).SetQueryParam(AuthenticationStateMiddleware.IdQueryParameterName, authStateHelper.AuthenticationState.JourneyId);
        var request = new HttpRequestMessage(method, fullUrl);

        if (content is not null)
        {
            request.Content = content;
        }

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        if (response.Content.Headers.ContentType?.MediaType == "text/html")
        {
            var doc = await response.GetDocument();
            Assert.Null(doc.GetElementByTestId("JourneyExpiredError"));
        }
    }

    public async Task ValidRequest_RendersContent(string url,
        Func<Configure, Func<AuthenticationState, Task>> configure)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(configure);

        var fullUrl = new Url(url).SetQueryParam(AuthenticationStateMiddleware.IdQueryParameterName, authStateHelper.AuthenticationState.JourneyId);
        var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        await response.GetDocument();
    }
}
