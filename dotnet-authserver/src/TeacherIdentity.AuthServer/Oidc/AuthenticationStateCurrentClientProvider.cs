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

        var authenticationState = _authenticationStateProvider.GetAuthenticationState(_httpContextAccessor.HttpContext);

        if (authenticationState is null)
        {
            return null;
        }

        var clientId = authenticationState.GetAuthorizationRequest().ClientId;
        var application = (Application?)await _applicationManager.FindByClientIdAsync(clientId!);

        return application;
    }
}
