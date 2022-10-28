using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.Authenticated.UpdateEmail;

public class ConfirmationModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly PinValidator _pinValidator;
    private readonly IClock _clock;

    public ConfirmationModel(
        TeacherIdentityServerDbContext dbContext,
        IEmailVerificationService emailVerificationService,
        PinValidator pinValidator,
        IClock clock)
    {
        _dbContext = dbContext;
        _emailVerificationService = emailVerificationService;
        _pinValidator = pinValidator;
        _clock = clock;
    }

    [BindProperty]
    [Display(Name = "Enter your code")]
    public string? Code { get; set; }

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
        Code = Code?.Trim();
        ValidateCode();

        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var verifyPinFailedReasons = await _emailVerificationService.VerifyPin(Email!.PlainValue, Code!);

        if (verifyPinFailedReasons != PinVerificationFailedReasons.None)
        {
            if (verifyPinFailedReasons == PinVerificationFailedReasons.RateLimitExceeded)
            {
                return new ViewResult()
                {
                    StatusCode = 429,
                    ViewName = "TooManyPinVerificationRequests"
                };
            }

            if (verifyPinFailedReasons.ShouldGenerateAnotherCode())
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

                ModelState.AddModelError(nameof(Code), "The security code has expired. New code sent.");
            }
            else
            {
                ModelState.AddModelError(nameof(Code), "Enter a correct security code");
            }

            return this.PageWithErrors();
        }

        var userId = User.GetUserId()!.Value;
        var user = await _dbContext.Users.SingleAsync(u => u.UserId == userId);

        if (user.EmailAddress != Email.PlainValue)
        {
            user.EmailAddress = Email.PlainValue;
            user.Updated = _clock.UtcNow;

            _dbContext.AddEvent(new Events.UserUpdatedEvent()
            {
                Source = Events.UserUpdatedEventSource.ChangedByUser,
                CreatedUtc = _clock.UtcNow,
                Changes = Events.UserUpdatedEventChanges.EmailAddress,
                User = Events.User.FromModel(user)
            });

            await _dbContext.SaveChangesAsync();

            if (HttpContext.TryGetAuthenticationState(out var authenticationState))
            {
                authenticationState.OnEmailChanged(Email!.PlainValue);
            }

            TempData.SetFlashSuccess("Email address updated");
        }

        var safeReturnUrl = !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ?
            ReturnUrl :
            "/";

        return Redirect(safeReturnUrl);
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (Email is null)
        {
            context.Result = new BadRequestResult();
        }
    }

    private void ValidateCode()
    {
        var validationError = _pinValidator.ValidateCode(Code);

        if (validationError is not null)
        {
            ModelState.AddModelError(nameof(Code), validationError);
        }
    }
}
