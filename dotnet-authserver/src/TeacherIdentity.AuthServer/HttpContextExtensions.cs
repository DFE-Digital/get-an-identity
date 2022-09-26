using System.Diagnostics.CodeAnalysis;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer;

public static class HttpContextExtensions
{
    public static AuthenticationState GetAuthenticationState(this HttpContext httpContext) =>
        TryGetAuthenticationState(httpContext, out var authenticationState) ?
            authenticationState :
            throw new InvalidOperationException($"The current request has no {nameof(AuthenticationState)}.");

    public static bool TryGetAuthenticationState(
        this HttpContext httpContext,
        [NotNullWhen(true)] out AuthenticationState? authenticationState)
    {
        authenticationState = httpContext.Features.Get<AuthenticationStateFeature>()?.AuthenticationState;
        return authenticationState is not null;
    }
}
