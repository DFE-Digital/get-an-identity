namespace TeacherIdentity.AuthServer.State;

public interface IAuthenticationStateProvider
{
    AuthenticationState? GetAuthenticationState(HttpContext httpContext);
    void SetAuthenticationState(HttpContext httpContext, AuthenticationState authenticationState);
}
