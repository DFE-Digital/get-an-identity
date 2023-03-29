using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.Authenticated.UpdateEmail;

public class ResendConfirmationModel : PageModel
{
    private readonly IUserVerificationService _userVerificationService;
    private readonly IdentityLinkGenerator _linkGenerator;

    public ResendConfirmationModel(
        IUserVerificationService userVerificationService,
        IdentityLinkGenerator linkGenerator)
    {
        _userVerificationService = userVerificationService;
        _linkGenerator = linkGenerator;
    }

    [FromQuery(Name = "email")]
    public ProtectedString? Email { get; set; }

    [FromQuery(Name = "cancelUrl")]
    public string? CancelUrl { get; set; }

    [FromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        var pinGenerationResult = await _userVerificationService.GenerateEmailPin(Email!.PlainValue);

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

        return Redirect(_linkGenerator.UpdateEmailConfirmation(Email!, ReturnUrl, CancelUrl));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (Email is null)
        {
            context.Result = new BadRequestResult();
        }
    }
}
