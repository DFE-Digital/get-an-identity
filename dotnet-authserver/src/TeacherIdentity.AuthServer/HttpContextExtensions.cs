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

    public static Task<ClaimsPrincipal> SignInCookies(
        this HttpContext httpContext,
        User user,
        TimeSpan? minExpires = null)
    {
        var newClaims = UserClaimHelper.GetInternalClaims(user);
        return SignInCookies(httpContext, newClaims, minExpires);
    }

    /// <summary>
    /// Signs in a user using the <see cref="CookieAuthenticationDefaults.AuthenticationScheme"/> scheme.
    /// </summary>
    /// <remarks>
    /// If the user is already signed in, <paramref name="newClaims"/> will be combined with the existing claims.
    /// </remarks>
    public static async Task<ClaimsPrincipal> SignInCookies(
        this HttpContext httpContext,
        IEnumerable<Claim> newClaims,
        TimeSpan? minExpires = null)
    {
        var scheme = CookieAuthenticationDefaults.AuthenticationScheme;

        var authenticateResult = await httpContext.AuthenticateAsync(scheme);

        ClaimsPrincipal principal;

        if (authenticateResult.Succeeded)
        {
            principal = authenticateResult.Principal!.Clone();

            // Replace claims in the existing principal with the new versions.
            // Any claim types that we don't know about are left intact.
            var newClaimTypes = newClaims.Select(c => c.Type);

            var identity = principal.Identities.Single();

            foreach (var claimType in newClaimTypes)
            {
                identity.RemoveClaims(claimType);
            }

            identity.AddClaims(newClaims);
        }
        else
        {
            principal = AuthenticationState.CreatePrincipal(newClaims);
        }

        var expires = GetExpires();

        var properties = new AuthenticationProperties()
        {
            IssuedUtc = authenticateResult.Properties?.IssuedUtc,
            ExpiresUtc = expires
        };

        await httpContext.SignInAsync(scheme, principal, properties);

        return principal;

        DateTimeOffset GetExpires()
        {
            var expiries = new List<DateTimeOffset>();

            if (authenticateResult.Properties?.ExpiresUtc is DateTimeOffset existingExpires)
            {
                expiries.Add(existingExpires);
            }

            if (minExpires is not null)
            {
                expiries.Add(DateTimeOffset.UtcNow.Add(minExpires.Value));
            }

            if (expiries.Count == 0)
            {
                throw new InvalidOperationException("Could not deduce expiry for authentication ticket.");
            }

            return expiries.Max();
        }
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

    public static async Task SaveUserSignedOutEvent(this HttpContext httpContext, ClaimsPrincipal principal, string? clientId)
    {
        await using var scope = httpContext.RequestServices.CreateAsyncScope();
        var clock = scope.ServiceProvider.GetRequiredService<IClock>();
        var dbContext = scope.ServiceProvider.GetRequiredService<TeacherIdentityServerDbContext>();

        var userId = principal.GetUserId()!.Value;
        var user = await dbContext.Users.FindAsync(userId);

        dbContext.AddEvent(new Events.UserSignedOutEvent()
        {
            ClientId = clientId,
            CreatedUtc = clock.UtcNow,
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
