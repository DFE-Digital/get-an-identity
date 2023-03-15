using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.State;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[AllowCompletedAuthenticationJourney]
public class PhoneExists : PageModel
{
    private IIdentityLinkGenerator _linkGenerator;

    public PhoneExists(IIdentityLinkGenerator linkGenerator)
    {
        _linkGenerator = linkGenerator;
    }

    public string? MobileNumber => HttpContext.GetAuthenticationState().MobileNumber;

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (!authenticationState.IsComplete())
        {
            context.Result = new RedirectResult(_linkGenerator.RegisterPhoneConfirmation());
        }
    }
}
