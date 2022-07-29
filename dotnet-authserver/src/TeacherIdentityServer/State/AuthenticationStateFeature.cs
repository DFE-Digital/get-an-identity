namespace TeacherIdentityServer.State;

public class AuthenticationStateFeature
{
    public AuthenticationStateFeature(AuthenticationState authenticationState)
    {
        AuthenticationState = authenticationState;
    }

    public AuthenticationState AuthenticationState { get; }
}
