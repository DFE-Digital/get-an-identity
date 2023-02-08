using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[RequireAuthenticationMilestone(AuthenticationState.AuthenticationMilestone.TrnLookupCompleted)]
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

        var pinGenerationResult = await _emailVerificationService.GeneratePin(email!);

        if (pinGenerationResult.FailedReasons != PinGenerationFailedReasons.None)
        {
            if (pinGenerationResult.FailedReasons == PinGenerationFailedReasons.RateLimitExceeded)
            {
                return new ViewResult()
                {
                    StatusCode = 429,
                    ViewName = "TooManyRequests"
                };
            }

            throw new NotImplementedException($"Unknown {nameof(PinGenerationFailedReasons)}: '{pinGenerationResult.FailedReasons}'.");
        }

        return Redirect(_linkGenerator.TrnInUse());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = HttpContext.GetAuthenticationState();

        if (authenticationState.TrnLookup != AuthenticationState.TrnLookupState.ExistingTrnFound)
        {
            context.Result = Redirect(authenticationState.GetNextHopUrl(_linkGenerator));
        }
    }
}
