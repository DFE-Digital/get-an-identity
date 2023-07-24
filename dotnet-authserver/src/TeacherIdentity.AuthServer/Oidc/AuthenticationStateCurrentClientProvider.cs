using Microsoft.AspNetCore;
using OpenIddict.Abstractions;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Oidc;

public class AuthenticationStateCurrentClientProvider : ICurrentClientProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IOpenIddictApplicationManager _applicationManager;

    public AuthenticationStateCurrentClientProvider(
        IHttpContextAccessor httpContextAccessor,
        IOpenIddictApplicationManager applicationManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _applicationManager = applicationManager;
    }

    public async Task<Application?> GetCurrentClient()
    {
        if (_httpContextAccessor.HttpContext is null)
        {
            return null;
        }

        var clientId = (_httpContextAccessor.HttpContext.TryGetAuthenticationState(out var authenticationState) ? authenticationState.OAuthState?.ClientId : null) ??
            _httpContextAccessor.HttpContext.GetOpenIddictServerRequest()?.ClientId;

        if (clientId is null)
        {
            return null;
        }

        var application = (Application?)await _applicationManager.FindByClientIdAsync(clientId);
        return application;
    }
}
