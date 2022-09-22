using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using TeacherIdentity.AuthServer.State;
using static OpenIddict.Abstractions.OpenIddictConstants;

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

    public static async Task SignInUserFromAuthenticationState(this HttpContext httpContext)
    {
        var authenticationState = httpContext.GetAuthenticationState();
        Debug.Assert(authenticationState.IsComplete());
        var claims = authenticationState.GetInternalClaims();

        var identity = new ClaimsIdentity(claims, authenticationType: "email", nameType: Claims.Name, roleType: null);
        var principal = new ClaimsPrincipal(identity);

        await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}
