using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TeacherIdentityServer.State;

public class RequireAuthenticationStateFilter : IPageFilter
{
    public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
    {
    }

    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (context.HttpContext.Features.Get<AuthenticationStateFeature>() is null)
        {
            context.Result = new BadRequestResult();
        }
    }

    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
    }
}
