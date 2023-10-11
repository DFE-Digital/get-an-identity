using Dfe.Analytics.AspNetCore;

namespace TeacherIdentity.AuthServer.Infrastructure.Middleware;

public class AppendAuthorizationInfoToAnalyticsEventsMiddleware
{
    private readonly RequestDelegate _next;

    public AppendAuthorizationInfoToAnalyticsEventsMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task Invoke(HttpContext context)
    {
        var analyticsEvent = context.GetWebRequestEvent();
        var clientRedirectInfo = context.GetClientRedirectInfo();

        if (context.TryGetAuthenticationState(out var authenticationState))
        {
            if (!string.IsNullOrEmpty(authenticationState.SessionId))
            {
                analyticsEvent.AddData("session_id", authenticationState.SessionId);
            }

            if (authenticationState.TryGetOAuthState(out var oAuthState))
            {
                analyticsEvent.AddData("client_id", oAuthState.ClientId);
            }
        }
        else if (clientRedirectInfo is not null)
        {
            analyticsEvent.AddData("client_id", clientRedirectInfo.ClientId);
        }

        return _next(context);
    }
}
