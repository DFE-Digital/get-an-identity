using Flurl;
using TeacherIdentity.AuthServer.Models;
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
        TrnRequirementType? trnRequirementType,
        HttpMethod method,
        string url,
        HttpContent? content = null)
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true);
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Completed(user, firstTimeSignIn: true), additionalScopes, trnRequirementType);

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
        TrnRequirementType? trnRequirementType,
        HttpMethod method,
        string url,
        HttpContent? content = null)
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true);
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Completed(user, firstTimeSignIn: true), additionalScopes, trnRequirementType);

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
        AuthenticationStateConfiguration configureAuthenticationHelper,
        string? additionalScopes,
        TrnRequirementType? trnRequirementType,
        HttpMethod method,
        string url,
        HttpContent? content = null,
        TrnMatchPolicy? trnMatchPolicy = null)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(
            c => async s =>
            {
                s.Reset(Clock.UtcNow.Subtract(AuthenticationState.JourneyLifetime));
                await configureAuthenticationHelper(c)(s);
            },
            additionalScopes,
            trnRequirementType,
            trnMatchPolicy);

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
        TrnRequirementType? trnRequirementType,
        HttpMethod method,
        string url,
        HttpContent? content = null)
    {
        // Arrange
        var user = await TestData.CreateUser(hasTrn: true);
        var authStateHelper = await CreateAuthenticationStateHelper(
            c => async s =>
            {
                s.Reset(Clock.UtcNow.Subtract(AuthenticationState.JourneyLifetime));
                await configureAuthenticationHelper(c)(s);
            },
            additionalScopes,
            trnRequirementType);

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

    public async Task JourneyRequiresTrnLookup_TrnLookupRequiredIsFalse_ReturnsBadRequest(
        AuthenticationStateConfiguration configureAuthenticationHelper,
        HttpMethod method,
        string url,
        HttpContent? content = null)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(configureAuthenticationHelper, additionalScopes: null, trnRequirementType: null);

        var fullUrl = $"{url}?{authStateHelper.ToQueryParam()}";
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

    public async Task ValidRequest_RendersContent(
        AuthenticationStateConfiguration configureAuthenticationHelper,
        string? additionalScopes,
        TrnRequirementType? trnRequirementType,
        string url)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(configureAuthenticationHelper, additionalScopes, trnRequirementType);

        var fullUrl = new Url(url).SetQueryParam(AuthenticationStateMiddleware.IdQueryParameterName, authStateHelper.AuthenticationState.JourneyId);
        var request = new HttpRequestMessage(HttpMethod.Get, fullUrl);

        // Act
        var response = await HttpClient.SendAsync(request);

        // Assert
        response.EnsureSuccessStatusCode();
        await response.GetDocument();
    }

    public async Task GivenAuthenticationState_RedirectsTo(
        AuthenticationStateConfiguration configureAuthenticationHelper,
        string? additionalScopes,
        TrnRequirementType? trnRequirementType,
        HttpMethod method,
        string url,
        string redirectUrl,
        HttpContent? content = null)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(configureAuthenticationHelper, additionalScopes, trnRequirementType);

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
        Assert.StartsWith(redirectUrl, response.Headers.Location?.OriginalString);
    }

    public async Task InvalidUserRequirements_ReturnsBadRequest(
        AuthenticationStateConfiguration configureAuthenticationHelper,
        string? additionalScopes,
        TrnRequirementType? trnRequirementType,
        HttpMethod method,
        string url,
        HttpContent? content = null)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(configureAuthenticationHelper, additionalScopes, trnRequirementType);

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
    }

    public async Task NoEmail_RedirectsToEmailPage(
        string? additionalScopes,
        TrnRequirementType? trnRequirementType,
        HttpMethod method,
        string url,
        HttpContent? content = null)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.Start(), additionalScopes, trnRequirementType);

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
        Assert.Equal($"/sign-in/email?{authStateHelper.ToQueryParam()}", response.Headers.Location?.OriginalString);
    }

    public async Task NoVerifiedEmail_RedirectsToEmailConfirmationPage(
        string? additionalScopes,
        TrnRequirementType? trnRequirementType,
        HttpMethod method,
        string url,
        HttpContent? content = null)
    {
        // Arrange
        var authStateHelper = await CreateAuthenticationStateHelper(c => c.EmailSet(), additionalScopes, trnRequirementType);

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
        Assert.Equal($"/sign-in/email-confirmation?{authStateHelper.ToQueryParam()}", response.Headers.Location?.OriginalString);
    }
}
