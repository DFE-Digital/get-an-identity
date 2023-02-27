using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Helpers;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[BindProperties]
public class Phone : PageModel
{
    private readonly IUserVerificationService _userVerificationService;
    private readonly IIdentityLinkGenerator _linkGenerator;

    public Phone(
        IUserVerificationService userVerificationService,
        IIdentityLinkGenerator linkGenerator)
    {
        _userVerificationService = userVerificationService;
        _linkGenerator = linkGenerator;
    }

    [Display(Name = "Mobile number", Description = "For international numbers include the country code")]
    [Required(ErrorMessage = "Enter your mobile phone number")]
    [Phone(ErrorMessage = "Enter a valid mobile phone number")]
    public string? MobileNumber { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var pinGenerationResult = await _userVerificationService.GenerateSmsPin(PhoneHelper.FormatMobileNumber(MobileNumber!));

        switch (pinGenerationResult.FailedReasons)
        {
            case PinGenerationFailedReasons.None:
                break;

            case PinGenerationFailedReasons.RateLimitExceeded:
                return new ViewResult()
                {
                    StatusCode = 429,
                    ViewName = "TooManyRequests"
                };

            case PinGenerationFailedReasons.InvalidAddress:
                ModelState.AddModelError(nameof(MobileNumber), "Enter a valid mobile phone number");
                return this.PageWithErrors();

            default:
                throw new NotImplementedException($"Unknown {nameof(PinGenerationFailedReasons)}: '{pinGenerationResult.FailedReasons}'.");
        }

        HttpContext.GetAuthenticationState().OnMobileNumberSet(MobileNumber!);

        return Redirect(_linkGenerator.RegisterPhoneConfirmation());
    }
}
