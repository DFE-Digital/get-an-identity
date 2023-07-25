namespace TeacherIdentity.AuthServer.State;

public interface IAuthenticationStateProvider
{
    Task<AuthenticationState?> GetAuthenticationState(HttpContext httpContext);
    Task SetAuthenticationState(HttpContext httpContext, AuthenticationState authenticationState);
}
