using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[AllowCompletedAuthenticationJourney]
public class EmailExists : PageModel
{
    private IIdentityLinkGenerator _linkGenerator;

    public EmailExists(IIdentityLinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }
    public string? Email => HttpContext.GetAuthenticationState().EmailAddress;

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (!authenticationState.IsComplete())
        {
            context.Result = new RedirectResult(_linkGenerator.RegisterEmailConfirmation());
        }
    }
}
