using System.Diagnostics;
using Flurl;
using TeacherIdentity.AuthServer.Oidc;
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

    public async Task JourneyIsAlreadyCompleted_RedirectsToPostSignInUrl(
        string? additionalScopes,
        HttpMethod method,
        string url,
        HttpContent? content = null)
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true);
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Completed(user, firstTimeSignIn: true), additionalScopes);

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

    public async Task JourneyIsAlreadyCompleted_DoesNotRedirectToPostSignInUrl(
        string? additionalScopes,
        HttpMethod method,
        string url,
        HttpContent? content = null)
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true);
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Completed(user, firstTimeSignIn: true), additionalScopes);

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
        string? additionalScopes,
        HttpMethod method,
        string url,
        HttpContent? content = null)
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true);
        var authStateHelper = await CreateAuthenticationStateHelper(
            c => async s =>
            {
                s.Reset(Clock.UtcNow.Subtract(TimeSpan.FromMinutes(20)));
                await configureAuthenticationHelper(c)(s);
            },
            additionalScopes);

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
        string? additionalScopes,
        HttpMethod method,
        string url,
        HttpContent? content = null)
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true);
        var authStateHelper = await CreateAuthenticationStateHelper(
            c => async s =>
            {
                s.Reset(Clock.UtcNow.Subtract(TimeSpan.FromMinutes(20)));
                await configureAuthenticationHelper(c)(s);
            },
            additionalScopes);

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

    public async Task ValidRequest_RendersContent(
        Func<Configure, Func<AuthenticationState, Task>> configureAuthenticationHelper,
        string url,
        string? additionalScopes)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(configureAuthenticationHelper, additionalScopes);

        var fullUrl = new Url(url).SetQueryParam(AuthenticationStateMiddleware.IdQueryParameterName, authStateHelper.AuthenticationState.JourneyId);
        var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        await response.GetDocument();
    }

    public async Task JourneyMilestoneHasPassed_RedirectsToStartOfNextMilestone(
        AuthenticationState.AuthenticationMilestone milestone,
        HttpMethod method,
        string url,
        HttpContent? content = null)
    {
        // Arrange
        AuthenticationStateHelper authStateHelper;

        switch (milestone)
        {
            case AuthenticationState.AuthenticationMilestone.None:
                authStateHelper = await CreateAuthenticationStateHelper(c => c.Start(), CustomScopes.DqtRead);
                break;

            case AuthenticationState.AuthenticationMilestone.EmailVerified:
                authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailVerified(), CustomScopes.DqtRead);
                break;

            case AuthenticationState.AuthenticationMilestone.TrnLookupCompleted:
                var user = await TestData.CreateUser(hasTrn: true);
                authStateHelper = await CreateAuthenticationStateHelper(
                    c => c.TrnLookupCompletedForExistingTrn(newEmail: Faker.Internet.Email(), trnOwner: user),
                    CustomScopes.DqtRead);
                break;

            default:
                throw new NotImplementedException($"Unknown {nameof(AuthenticationState.AuthenticationMilestone)}: '{milestone}'.");
        };

        Debug.Assert(authStateHelper.AuthenticationState.GetLastMilestone() == milestone);

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
        Assert.Equal(authStateHelper.GetNextHopUrl(), response.Headers.Location?.OriginalString);
    }

    public async Task InvalidUserRequirements_ReturnsForbidden(
        Func<Configure, Func<AuthenticationState, Task>> configureAuthenticationHelper,
        string? additionalScopes,
        HttpMethod method,
        string url,
        HttpContent? content = null)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(configureAuthenticationHelper, additionalScopes);

        var fullUrl = new Url(url).SetQueryParam(AuthenticationStateMiddleware.IdQueryParameterName, authStateHelper.AuthenticationState.JourneyId);
        var request = new HttpRequestMessage(method, fullUrl);

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, (int)response.StatusCode);
    }
}
