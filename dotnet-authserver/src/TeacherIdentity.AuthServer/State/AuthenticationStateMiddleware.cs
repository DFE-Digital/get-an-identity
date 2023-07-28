using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Extensions;
using Sentry;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.State;

public class AuthenticationStateMiddleware
{
    public const string IdQueryParameterName = "asid";

    private readonly RequestDelegate _next;

    public AuthenticationStateMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IAuthenticationStateProvider authenticationStateProvider)
    {
        // If we're re-executing because of an error, don't reload state
        if (context.Features.Get<IStatusCodeReExecuteFeature>() is not null)
        {
            await _next(context);
            return;
        }

        var authenticationState = await authenticationStateProvider.GetAuthenticationState(context);

        if (authenticationState is not null)
        {
            if (context.Items.TryAdd(typeof(HaveAddedUrlToVisitedMarker), HaveAddedUrlToVisitedMarker.Instance))
            {
                authenticationState.Visited.Add($"{context.Request.Method} {context.Request.GetEncodedPathAndQuery()}");
            }

            context.Features.Set(new AuthenticationStateFeature(authenticationState));
        }

        try
        {
            await _next(context);
        }
        catch (Exception) when (authenticationState is not null)
        {
            // Snapshot the AuthenticationState and stash it in the DB for subsequent debugging

            var snapshotId = Guid.NewGuid();
            var clock = context.RequestServices.GetRequiredService<IClock>();

            using (var dbContext = context.RequestServices.GetRequiredService<TeacherIdentityServerDbContext>())
            {
                var payload = authenticationState.Serialize();

                dbContext.AuthenticationStateSnapshots.Add(new()
                {
                    Created = clock.UtcNow,
                    JourneyId = authenticationState.JourneyId,
                    SnapshotId = snapshotId,
                    Payload = payload
                });

                await dbContext.SaveChangesAsync();
            }

            SentrySdk.ConfigureScope(scope => scope.SetTag("authentication_state_snapshot.id", snapshotId.ToString()));

            throw;
        }

        var authenticationStateFeature = context.Features.Get<AuthenticationStateFeature>();

        if (authenticationStateFeature is not null)
        {
            if (authenticationStateFeature.AuthenticationState.JourneyId == default)
            {
                throw new InvalidOperationException($"{nameof(AuthenticationState)} must have {nameof(AuthenticationState.JourneyId)} set.");
            }

            await authenticationStateProvider.SetAuthenticationState(context, authenticationStateFeature.AuthenticationState);
        }
    }

    private sealed class HaveAddedUrlToVisitedMarker
    {
        private HaveAddedUrlToVisitedMarker() { }

        public static HaveAddedUrlToVisitedMarker Instance { get; } = new();
    }
}
