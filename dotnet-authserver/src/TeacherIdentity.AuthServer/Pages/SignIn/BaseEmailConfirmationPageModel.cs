using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class BaseEmailConfirmationPageModel : PageModel
{
    protected readonly PinValidator PinValidator;
    protected readonly IEmailVerificationService EmailVerificationService;

    public BaseEmailConfirmationPageModel(
        IEmailVerificationService emailVerificationService,
        PinValidator pinValidator)
    {
        PinValidator = pinValidator;
        EmailVerificationService = emailVerificationService;
    }

    public virtual string? Email => HttpContext.GetAuthenticationState().EmailAddress;

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
            var pinGenerationResult = await EmailVerificationService.GeneratePin(Email!);

            if (pinGenerationResult.FailedReasons != PinGenerationFailedReasons.None)
            {
                HandlePinGenerationFailed(pinGenerationResult.FailedReasons);
            }

            ModelState.AddModelError(nameof(Code), "The security code has expired. New code sent.");
        }
        else
        {
            ModelState.AddModelError(nameof(Code), "Enter a correct security code");
        }

        return this.PageWithErrors();
    }

    private IActionResult HandlePinGenerationFailed(PinGenerationFailedReasons pinGenerationFailedReasons)
    {
        if (pinGenerationFailedReasons == PinGenerationFailedReasons.RateLimitExceeded)
        {
            return new ViewResult()
            {
                StatusCode = 429,
                ViewName = "TooManyRequests"
            };
        }

        throw new NotImplementedException($"Unknown {nameof(PinGenerationFailedReasons)}: '{pinGenerationFailedReasons}'.");
    }

    protected void ValidateCode()
    {
        var validationError = PinValidator.ValidateCode(Code);

        if (validationError is not null)
        {
            ModelState.AddModelError(nameof(Code), validationError);
        }
    }
}
