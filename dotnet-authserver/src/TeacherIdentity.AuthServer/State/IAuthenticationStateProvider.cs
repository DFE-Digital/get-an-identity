namespace TeacherIdentity.AuthServer.State;

public interface IAuthenticationStateProvider
{
    void ClearAuthenticationState(HttpContext httpContext);
    AuthenticationState? GetAuthenticationState(HttpContext httpContext);
    void SetAuthenticationState(HttpContext httpContext, AuthenticationState authenticationState);
}
