using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.Authenticated.UpdateEmail;

public class ResendConfirmationModel : PageModel
{
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly IIdentityLinkGenerator _linkGenerator;

    public ResendConfirmationModel(
        IEmailVerificationService emailVerificationService,
        IIdentityLinkGenerator linkGenerator)
    {
        _emailVerificationService = emailVerificationService;
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
        var pinGenerationResult = await _emailVerificationService.GeneratePin(Email!.PlainValue);

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
