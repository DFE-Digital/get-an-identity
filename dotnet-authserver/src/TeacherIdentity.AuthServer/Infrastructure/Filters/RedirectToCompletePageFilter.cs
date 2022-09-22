using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TeacherIdentity.AuthServer.Infrastructure.Filters;

public class RedirectToCompletePageFilter : IPageFilter
{
    public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
    {
    }

    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (context.HttpContext.TryGetAuthenticationState(out var authenticationState) && authenticationState.IsComplete())
        {
            var linkGenerator = context.HttpContext.RequestServices.GetRequiredService<IIdentityLinkGenerator>();

            var completeUrl = linkGenerator.CompleteAuthorization();

            if (completeUrl != context.HttpContext.Request.GetEncodedPathAndQuery())
            {
                authenticationState.OnHaveResumedCompletedJourney();
                context.Result = new RedirectResult(completeUrl);
            }
        }
    }

    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
    }
}
