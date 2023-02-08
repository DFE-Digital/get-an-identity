using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class RequireAuthenticationMilestoneAttribute : Attribute, IPageFilter, IOrderedFilter
{
    public RequireAuthenticationMilestoneAttribute(AuthenticationState.AuthenticationMilestone milestone)
    {
        Milestone = milestone;
    }

    public AuthenticationState.AuthenticationMilestone Milestone { get; }

    public int Order => int.MinValue;

    public void OnPageHandlerExecuted(PageHandlerExecutedContext context)
    {
    }

    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (authenticationState.GetLastMilestone() != Milestone)
        {
            var linkGenerator = context.HttpContext.RequestServices.GetRequiredService<IIdentityLinkGenerator>();
            context.Result = new RedirectResult(authenticationState.GetNextHopUrl(linkGenerator));
        }
    }

    public void OnPageHandlerSelected(PageHandlerSelectedContext context)
    {
    }
}
