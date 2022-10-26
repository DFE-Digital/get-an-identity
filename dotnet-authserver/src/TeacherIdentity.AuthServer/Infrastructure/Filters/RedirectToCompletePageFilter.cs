using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Infrastructure.Filters;

public class RedirectToCompletePageFilterFactory : IFilterFactory
{
    public bool IsReusable => true;

    public IFilterMetadata CreateInstance(IServiceProvider serviceProvider)
    {
        var filter = serviceProvider.GetRequiredService<RedirectToCompletePageFilter>();
        return filter;
    }
}

public class RedirectToCompletePageFilter : IPageFilter
{
    private readonly ILogger<RedirectToCompletePageFilter> _logger;

    public RedirectToCompletePageFilter(ILogger<RedirectToCompletePageFilter> logger)
    {
        _logger = logger;
    }

    public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
    {
    }

    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (context.HttpContext.TryGetAuthenticationState(out var authenticationState) && authenticationState.IsComplete())
        {
            if (context.HttpContext.GetEndpoint()?.Metadata.Contains(AllowCompletedAuthenticationJourneyMarker.Instance) == true)
            {
                return;
            }

            var linkGenerator = context.HttpContext.RequestServices.GetRequiredService<IIdentityLinkGenerator>();

            var completeUrl = linkGenerator.CompleteAuthorization();

            if (completeUrl != context.HttpContext.Request.GetEncodedPathAndQuery())
            {
                _logger.LogDebug("Authentication journey is completed; redirecting to completion URL.");

                authenticationState.OnHaveResumedCompletedJourney();
                context.Result = new RedirectResult(completeUrl);
            }
        }
    }

    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
    }
}
