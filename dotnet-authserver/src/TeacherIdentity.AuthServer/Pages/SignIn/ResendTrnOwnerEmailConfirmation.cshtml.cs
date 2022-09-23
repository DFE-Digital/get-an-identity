using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class ResendTrnOwnerEmailConfirmationModel : PageModel
{
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly IIdentityLinkGenerator _linkGenerator;

    public ResendTrnOwnerEmailConfirmationModel(
        IEmailVerificationService emailVerificationService,
        IIdentityLinkGenerator linkGenerator)
    {
        _emailVerificationService = emailVerificationService;
        _linkGenerator = linkGenerator;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        var email = HttpContext.GetAuthenticationState().TrnOwnerEmailAddress!;

        HttpContext.GetAuthenticationState().OnEmailSet(email);

        await _emailVerificationService.GeneratePin(email!);

        return Redirect(_linkGenerator.TrnInUse());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = HttpContext.GetAuthenticationState();

        if (string.IsNullOrEmpty(authenticationState.EmailAddress) ||
            !authenticationState.EmailAddressVerified ||
            authenticationState.TrnLookup != AuthenticationState.TrnLookupState.ExistingTrnFound)
        {
            context.Result = Redirect(authenticationState.GetNextHopUrl(_linkGenerator));
        }
    }
}
