using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class BaseExistingPhonePageModel : PageModel
{
    private IUserVerificationService _userVerificationService;

    public BaseExistingPhonePageModel(IUserVerificationService userVerificationService)
    {
        _userVerificationService = userVerificationService;
    }

    public async Task<PinGenerationResultAction> GenerateSmsPinForExistingMobileNumber(string mobileNumber)
    {
        var pinGenerationResult = await _userVerificationService.GenerateSmsPin(mobileNumber);

        switch (pinGenerationResult.FailedReason)
        {
            case PinGenerationFailedReason.None:
                return PinGenerationResultAction.Succeeded();

            case PinGenerationFailedReason.RateLimitExceeded:
                return PinGenerationResultAction.Failed(new ViewResult()
                {
                    StatusCode = 429,
                    ViewName = "TooManyRequests"
                });

            default:
                throw new NotImplementedException($"Unknown {nameof(PinGenerationFailedReason)}: '{pinGenerationResult.FailedReason}'.");
        }
    }
}
