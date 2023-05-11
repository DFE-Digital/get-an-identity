using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Tests.Infrastructure;

public class TestAuthenticationStateProvider : IAuthenticationStateProvider
{
    private readonly ConcurrentDictionary<string, AuthenticationState> _state = new();

    public void ClearAllAuthenticationState() => _state.Clear();

    public void ClearAuthenticationState(HttpContext httpContext)
    {
        if (httpContext.Request.Query.TryGetValue(AuthenticationStateMiddleware.IdQueryParameterName, out var asid))
        {
            _state.Remove(asid.ToString(), out _);
        }
    }

    public IReadOnlyDictionary<string, AuthenticationState> GetAllAuthenticationState() =>
        new ReadOnlyDictionary<string, AuthenticationState>(_state);

    public AuthenticationState? GetAuthenticationState(HttpContext httpContext) =>
        httpContext.Request.Query.TryGetValue(AuthenticationStateMiddleware.IdQueryParameterName, out var asid) &&
            _state.TryGetValue(asid.ToString(), out var authenticationState) ?
                authenticationState :
                null;

    public AuthenticationState? GetAuthenticationState(Guid journeyId) =>
        _state.TryGetValue(journeyId.ToString(), out var authState) ? authState : null;

    public void SetAuthenticationState(AuthenticationState authenticationState) =>
        _state[authenticationState.JourneyId.ToString()] = authenticationState;

    public void SetAuthenticationState(HttpContext? httpContext, AuthenticationState authenticationState) =>
        SetAuthenticationState(authenticationState);
}
