using Microsoft.AspNetCore;
using OpenIddict.Abstractions;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Oidc;

public class AuthenticationStateCurrentClientProvider : ICurrentClientProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthenticationStateProvider _authenticationStateProvider;
    private readonly IOpenIddictApplicationManager _applicationManager;

    public AuthenticationStateCurrentClientProvider(
        IHttpContextAccessor httpContextAccessor,
        IAuthenticationStateProvider authenticationStateProvider,
        IOpenIddictApplicationManager applicationManager)
    {
        _httpContextAccessor = httpContextAccessor;
        _authenticationStateProvider = authenticationStateProvider;
        _applicationManager = applicationManager;
    }

    public async Task<Application?> GetCurrentClient()
    {
        if (_httpContextAccessor.HttpContext is null)
        {
            return null;
        }

        var authorizationRequest = _authenticationStateProvider.GetAuthenticationState(_httpContextAccessor.HttpContext)?.GetAuthorizationRequest() ??
            _httpContextAccessor.HttpContext.GetOpenIddictServerRequest();

        if (authorizationRequest is null)
        {
            return null;
        }

        var application = (Application?)await _applicationManager.FindByClientIdAsync(authorizationRequest.ClientId!);
        return application;
    }
}
