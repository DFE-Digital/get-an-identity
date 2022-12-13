using Dfe.Analytics.AspNetCore;

namespace TeacherIdentity.AuthServer.Infrastructure.Middleware;

public class AppendSessionIdToAnalyticsEventsMiddleware
{
    private readonly RequestDelegate _next;

    public AppendSessionIdToAnalyticsEventsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task Invoke(HttpContext context)
    {
        var analyticsFeature = context.Features.Get<WebRequestEventFeature>();

        if (analyticsFeature is not null &&
            context.TryGetAuthenticationState(out var authenticationState) &&
            !string.IsNullOrEmpty(authenticationState.SessionId))
        {
            var analyticsEvent = context.GetWebRequestEvent();
            analyticsEvent.AddData("session_id", authenticationState.SessionId);
        }

        return _next(context);
    }
}
