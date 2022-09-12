using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Routing;

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
            var urlHelperFactory = context.HttpContext.RequestServices.GetRequiredService<IUrlHelperFactory>();
            var urlHelper = urlHelperFactory.GetUrlHelper(context);

            var completeUrl = urlHelper.CompleteAuthorization();

            if (completeUrl != context.HttpContext.Request.GetEncodedPathAndQuery())
            {
                authenticationState.HaveResumedCompletedJourney = true;
                context.Result = new RedirectResult(completeUrl);
            }
        }
    }

    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
    }
}
