using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Infrastructure.Filters;

public class RequireAuthenticationStateFilter : IAuthorizationFilter
{
    private readonly SignInJourneyProvider _signInJourneyProvider;
    private readonly IdentityLinkGenerator _linkGenerator;
    private readonly ILogger<RequireAuthenticationStateFilter> _logger;
    private readonly IClock _clock;

    public RequireAuthenticationStateFilter(
        SignInJourneyProvider signInJourneyProvider,
        IdentityLinkGenerator linkGenerator,
        ILogger<RequireAuthenticationStateFilter> logger,
        IClock clock)
    {
        _signInJourneyProvider = signInJourneyProvider;
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
        var signInJourney = _signInJourneyProvider.GetSignInJourney(authenticationState, context.HttpContext);

        // If the journey has been completed then forward to the completion page
        // (prevents going 'back' to amend submitted details)
        if (signInJourney.IsCompleted())
        {
            if (context.HttpContext.GetEndpoint()?.Metadata.Contains(AllowCompletedAuthenticationJourneyMarker.Instance) != true)
            {
                var completeUrl = authenticationState.PostSignInUrl;

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
