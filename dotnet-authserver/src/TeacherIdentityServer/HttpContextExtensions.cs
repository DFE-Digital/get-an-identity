using TeacherIdentityServer.State;

namespace TeacherIdentityServer;

public static class HttpContextExtensions
{
    public static AuthenticationState GetAuthenticationState(this HttpContext httpContext) =>
        httpContext.Features.Get<AuthenticationStateFeature>()?.AuthenticationState ??
            throw new InvalidOperationException($"The current request has no {nameof(AuthenticationState)}.");
}
