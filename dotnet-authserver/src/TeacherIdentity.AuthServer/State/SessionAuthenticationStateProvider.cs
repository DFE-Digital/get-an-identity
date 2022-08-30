namespace TeacherIdentity.AuthServer.State;

public class SessionAuthenticationStateProvider : IAuthenticationStateProvider
{
    private readonly ILogger<SessionAuthenticationStateProvider> _logger;

    public SessionAuthenticationStateProvider(ILogger<SessionAuthenticationStateProvider> logger)
    {
        _logger = logger;
    }

    public void ClearAuthenticationState(HttpContext httpContext)
    {
        if (httpContext.Request.Query.TryGetValue(AuthenticationStateMiddleware.IdQueryParameterName, out var asidStr) &&
            Guid.TryParse(asidStr, out var asid))
        {
            var sessionKey = GetSessionKey(asid);
            httpContext.Session.Remove(sessionKey);
        }
    }

    public AuthenticationState? GetAuthenticationState(HttpContext httpContext)
    {
        if (httpContext.Request.Query.TryGetValue(AuthenticationStateMiddleware.IdQueryParameterName, out var asidStr) &&
            Guid.TryParse(asidStr, out var asid))
        {
            var sessionKey = GetSessionKey(asid);
            var serializedAuthenticationState = httpContext.Session.GetString(sessionKey);

            if (serializedAuthenticationState is not null)
            {
                try
                {
                    var authenticationState = AuthenticationState.Deserialize(serializedAuthenticationState);
                    return authenticationState;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Failed deserializing {nameof(AuthenticationState)}.");
                }
            }
        }

        return null;
    }

    public void SetAuthenticationState(HttpContext httpContext, AuthenticationState authenticationState)
    {
        var sessionKey = GetSessionKey(authenticationState.JourneyId);
        var serializedAuthenticationState = authenticationState.Serialize();
        httpContext.Session.SetString(sessionKey, serializedAuthenticationState);
    }

    private static string GetSessionKey(Guid asid) => $"auth-state:{asid:N}";
}
