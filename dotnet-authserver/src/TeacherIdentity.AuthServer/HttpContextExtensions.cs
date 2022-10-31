using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using OpenIddict.Abstractions;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer;

public static class HttpContextExtensions
{
    public static AuthenticationState GetAuthenticationState(this HttpContext httpContext) =>
        TryGetAuthenticationState(httpContext, out var authenticationState) ?
            authenticationState :
            throw new InvalidOperationException($"The current request has no {nameof(AuthenticationState)}.");

    public static async Task ReSignInCookies(this HttpContext httpContext, User user)
    {
        var scheme = CookieAuthenticationDefaults.AuthenticationScheme;

        var authenticateResult = await httpContext.AuthenticateAsync(scheme);

        if (!authenticateResult.Succeeded)
        {
            throw new InvalidOperationException($"User is not authenticated with the '{scheme}' scheme.");
        }

        var newPrincipal = authenticateResult.Principal.Clone();

        // Replace claims in the existing principal with the new versions.
        // Any claim types that we don't know about are left intact.

        var newClaims = UserClaimHelper.GetInternalClaims(user);
        var newClaimTypes = newClaims.Select(c => c.Type);

        var identity = newPrincipal.Identities.Single();

        foreach (var claimType in newClaimTypes)
        {
            identity.RemoveClaims(claimType);
        }

        identity.AddClaims(newClaims);

        await httpContext.SignInAsync(scheme, newPrincipal, authenticateResult.Properties);
    }

    public static async Task SaveUserSignedInEvent(this HttpContext httpContext, ClaimsPrincipal principal)
    {
        httpContext.TryGetAuthenticationState(out var authenticationState);

        await using var scope = httpContext.RequestServices.CreateAsyncScope();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var dbContext = scope.ServiceProvider.GetRequiredService<TeacherIdentityServerDbContext>();

        var userId = principal.GetUserId()!.Value;
        var user = await dbContext.Users.FindAsync(userId);

        dbContext.AddEvent(new Events.UserSignedInEvent()
        {
            ClientId = authenticationState?.OAuthState?.ClientId,
            CreatedUtc = clock.UtcNow,
            Scope = authenticationState?.OAuthState?.Scope,
            User = user!
        });

        await dbContext.SaveChangesAsync();
    }

    public static bool TryGetAuthenticationState(
        this HttpContext httpContext,
        [NotNullWhen(true)] out AuthenticationState? authenticationState)
    {
        authenticationState = httpContext.Features.Get<AuthenticationStateFeature>()?.AuthenticationState;
        return authenticationState is not null;
    }
}
