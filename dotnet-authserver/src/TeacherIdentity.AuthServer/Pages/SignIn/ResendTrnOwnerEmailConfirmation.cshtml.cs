using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[RequireAuthenticationMilestone(AuthenticationState.AuthenticationMilestone.TrnLookupCompleted)]
public class ResendTrnOwnerEmailConfirmationModel : PageModel
{
    private readonly IUserVerificationService _userVerificationService;
    private readonly IdentityLinkGenerator _linkGenerator;

    public ResendTrnOwnerEmailConfirmationModel(
        IUserVerificationService userVerificationService,
        IdentityLinkGenerator linkGenerator)
    {
        _userVerificationService = userVerificationService;
        _linkGenerator = linkGenerator;
    }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        var email = HttpContext.GetAuthenticationState().TrnOwnerEmailAddress!;

        var pinGenerationResult = await _userVerificationService.GenerateEmailPin(email!);

        if (pinGenerationResult.FailedReason != PinGenerationFailedReason.None)
        {
            if (pinGenerationResult.FailedReason == PinGenerationFailedReason.RateLimitExceeded)
            {
                return new ViewResult()
                {
                    StatusCode = 429,
                    ViewName = "TooManyRequests"
                };
            }

            throw new NotImplementedException($"Unknown {nameof(PinGenerationFailedReason)}: '{pinGenerationResult.FailedReason}'.");
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
