using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[BindProperties]
public class EmailModel : PageModel
{
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly IIdentityLinkGenerator _linkGenerator;

    public EmailModel(
        IEmailVerificationService emailVerificationService,
        IIdentityLinkGenerator linkGenerator)
    {
        _emailVerificationService = emailVerificationService;
        _linkGenerator = linkGenerator;
    }

    [Display(Name = "Enter your email address")]
    [Required(ErrorMessage = "Enter your email address")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string? Email { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        HttpContext.GetAuthenticationState().OnEmailSet(Email!);

        var pinGenerationResult = await _emailVerificationService.GeneratePin(Email!);

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

        return Redirect(_linkGenerator.EmailConfirmation());
    }
}
