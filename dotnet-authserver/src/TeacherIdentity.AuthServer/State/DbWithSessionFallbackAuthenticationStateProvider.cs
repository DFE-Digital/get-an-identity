namespace TeacherIdentity.AuthServer.State;

public class DbWithSessionFallbackAuthenticationStateProvider : IAuthenticationStateProvider
{
    private readonly SessionAuthenticationStateProvider _sessionProvider;
    private readonly DbAuthenticationStateProvider _dbProvider;
    private bool _useDbProvider;

    public DbWithSessionFallbackAuthenticationStateProvider(
        SessionAuthenticationStateProvider sessionProvider,
        DbAuthenticationStateProvider dbProvider)
    {
        _sessionProvider = sessionProvider;
        _dbProvider = dbProvider;
    }

    public async Task<AuthenticationState?> GetAuthenticationState(HttpContext httpContext)
    {
        var result = await _sessionProvider.GetAuthenticationState(httpContext);

        if (result is not null)
        {
            return result;
        }

        _useDbProvider = true;

        return await _dbProvider.GetAuthenticationState(httpContext);
    }

    public async Task SetAuthenticationState(HttpContext httpContext, AuthenticationState authenticationState)
    {
        if (_useDbProvider)
        {
            await _dbProvider.SetAuthenticationState(httpContext, authenticationState);
        }
        else
        {
            await _sessionProvider.SetAuthenticationState(httpContext, authenticationState);
        }
    }
}
