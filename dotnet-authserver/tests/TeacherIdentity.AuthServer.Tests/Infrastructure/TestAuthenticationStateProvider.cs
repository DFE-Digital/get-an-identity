using System.Collections.Concurrent;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Tests.Infrastructure;

public class TestAuthenticationStateProvider : IAuthenticationStateProvider
{
    private readonly ConcurrentDictionary<string, AuthenticationState> _state = new();

    public void ClearAuthenticationState(HttpContext httpContext)
    {
        if (httpContext.Request.Query.TryGetValue(AuthenticationStateMiddleware.IdQueryParameterName, out var asid))
        {
            _state.Remove(asid, out _);
        }
    }

    public AuthenticationState? GetAuthenticationState(HttpContext httpContext) =>
        httpContext.Request.Query.TryGetValue(AuthenticationStateMiddleware.IdQueryParameterName, out var asid) &&
            _state.TryGetValue(asid, out var authenticationState) ?
                authenticationState :
                null;

    public AuthenticationState? GetAuthenticationState(Guid journeyId) =>
        _state.TryGetValue(journeyId.ToString(), out var authState) ? authState : null;

    public void SetAuthenticationState(HttpContext? httpContext, AuthenticationState authenticationState) =>
        _state[authenticationState.JourneyId.ToString()] = authenticationState;
}
