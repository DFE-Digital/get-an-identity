using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Infrastructure.Filters;

public class RequireAuthenticationStateFilterFactory : IFilterFactory
{
    public bool IsReusable => false;  // RequireAuthenticationStateFilter needs an IClock which is transient

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var filter = serviceProvider.GetRequiredService<RequireAuthenticationStateFilter>();
        return filter;
    }
}

public class RequireAuthenticationStateFilter : IAuthorizationFilter
{
    private readonly IdentityLinkGenerator _linkGenerator;
    private readonly ILogger<RequireAuthenticationStateFilter> _logger;
    private readonly IClock _clock;

    public RequireAuthenticationStateFilter(
        IdentityLinkGenerator linkGenerator,
        ILogger<RequireAuthenticationStateFilter> logger,
        IClock clock)
    {
        _linkGenerator = linkGenerator;
        _logger = logger;
        _clock = clock;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var authenticationStateFeature = context.HttpContext.Features.Get<AuthenticationStateFeature>();

        if (authenticationStateFeature is null)
        {
            _logger.LogDebug("Request to {RequestUrl} is missing authentication state.", context.HttpContext.Request.GetEncodedUrl());
            context.Result = new BadRequestResult();
            return;
        }

        var authenticationState = authenticationStateFeature.AuthenticationState;

        // If the journey has been completed then forward to the completion page
        // (prevents going 'back' to amend submitted details)
        if (authenticationState.IsComplete)
        {
            if (context.HttpContext.GetEndpoint()?.Metadata.Contains(AllowCompletedAuthenticationJourneyMarker.Instance) != true)
            {
                var completeUrl = authenticationState.GetNextHopUrl(_linkGenerator);

                if (completeUrl != context.HttpContext.Request.GetEncodedPathAndQuery())
                {
                    _logger.LogDebug("Authentication journey is completed; redirecting to completion URL.");

                    authenticationState.OnHaveResumedCompletedJourney();
                    context.Result = new RedirectResult(completeUrl);
                    return;
                }
            }
        }

        if (authenticationState.HasExpired(_clock.UtcNow))
        {
            if (context.HttpContext.GetEndpoint()?.Metadata.Contains(AllowExpiredAuthenticationJourneyMarker.Instance) != true)
            {
                _logger.LogDebug("Authentication journey has expired.");

                context.Result = new ViewResult()
                {
                    ViewName = "JourneyExpiredError",
                    StatusCode = StatusCodes.Status400BadRequest
                };
            }
        }
    }
}
