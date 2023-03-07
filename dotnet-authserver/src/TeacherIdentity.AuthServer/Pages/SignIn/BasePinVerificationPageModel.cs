using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public abstract class BasePinVerificationPageModel : PageModel
{
    protected readonly PinValidator PinValidator;
    protected readonly IUserVerificationService UserVerificationService;

    protected BasePinVerificationPageModel(
        IUserVerificationService userVerificationService,
        PinValidator pinValidator)
    {
        PinValidator = pinValidator;
        UserVerificationService = userVerificationService;
    }

    [BindProperty]
    [Display(Name = "Enter your code")]
    public virtual string? Code { get; set; }

    protected async Task<IActionResult> HandlePinVerificationFailed(PinVerificationFailedReasons pinVerificationFailedReasons)
    {
        if (pinVerificationFailedReasons == PinVerificationFailedReasons.RateLimitExceeded)
        {
            return new ViewResult()
            {
                StatusCode = 429,
                ViewName = "TooManyPinVerificationRequests"
            };
        }

        if (pinVerificationFailedReasons.ShouldGenerateAnotherCode())
        {
            var pinGenerationResult = await GeneratePin();

            if (pinGenerationResult.FailedReason != PinGenerationFailedReason.None)
            {
                HandlePinGenerationFailed(pinGenerationResult.FailedReason);
            }

            ModelState.AddModelError(nameof(Code), "The security code has expired. New code sent.");
        }
        else
        {
            ModelState.AddModelError(nameof(Code), "Enter a correct security code");
        }

        return this.PageWithErrors();
    }

    private IActionResult HandlePinGenerationFailed(PinGenerationFailedReason pinGenerationFailedReason)
    {
        if (pinGenerationFailedReason == PinGenerationFailedReason.RateLimitExceeded)
        {
            return new ViewResult()
            {
                StatusCode = 429,
                ViewName = "TooManyRequests"
            };
        }

        throw new NotImplementedException($"Unknown {nameof(PinGenerationFailedReason)}: '{pinGenerationFailedReason}'.");
    }

    protected void ValidateCode()
    {
        var validationError = PinValidator.ValidateCode(Code);

        if (validationError is not null)
        {
            ModelState.AddModelError(nameof(Code), validationError);
        }
    }

    protected abstract Task<PinGenerationResult> GeneratePin();
}
