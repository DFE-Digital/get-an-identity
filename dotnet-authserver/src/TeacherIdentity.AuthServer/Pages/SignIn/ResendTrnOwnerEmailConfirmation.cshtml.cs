using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[CheckCanAccessStep(CurrentStep)]
public class ResendTrnOwnerEmailConfirmationModel : PageModel
{
    private const string CurrentStep = SignInJourney.Steps.TrnInUseResendTrnOwnerEmailConfirmation;

    private readonly SignInJourney _signInJourney;
    private readonly IUserVerificationService _userVerificationService;

    public ResendTrnOwnerEmailConfirmationModel(
        SignInJourney signInJourney,
        IUserVerificationService userVerificationService)
    {
        _signInJourney = signInJourney;
        _userVerificationService = userVerificationService;
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

        return Redirect(_signInJourney.GetNextStepUrl(CurrentStep));
    }
}
