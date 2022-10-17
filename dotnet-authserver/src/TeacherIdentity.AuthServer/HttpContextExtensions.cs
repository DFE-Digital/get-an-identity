using System.Diagnostics.CodeAnalysis;
using System.Security.Claims;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer;

public static class HttpContextExtensions
{
    public static AuthenticationState GetAuthenticationState(this HttpContext httpContext) =>
        TryGetAuthenticationState(httpContext, out var authenticationState) ?
            authenticationState :
            throw new InvalidOperationException($"The current request has no {nameof(AuthenticationState)}.");

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
