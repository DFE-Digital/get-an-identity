using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.Authenticated.UpdateEmail;

public class ConfirmationModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IUserVerificationService _userVerificationService;
    private readonly PinValidator _pinValidator;
    private readonly IIdentityLinkGenerator _linkGenerator;
    private readonly IClock _clock;

    public ConfirmationModel(
        TeacherIdentityServerDbContext dbContext,
        IUserVerificationService userVerificationService,
        PinValidator pinValidator,
        IIdentityLinkGenerator linkGenerator,
        IClock clock)
    {
        _dbContext = dbContext;
        _userVerificationService = userVerificationService;
        _pinValidator = pinValidator;
        _linkGenerator = linkGenerator;
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

        var VerifyEmailPinFailedReasons = await _userVerificationService.VerifyEmailPin(Email!.PlainValue, Code!);

        if (VerifyEmailPinFailedReasons != PinVerificationFailedReasons.None)
        {
            if (VerifyEmailPinFailedReasons == PinVerificationFailedReasons.RateLimitExceeded)
            {
                return new ViewResult()
                {
                    StatusCode = 429,
                    ViewName = "TooManyPinVerificationRequests"
                };
            }

            if (VerifyEmailPinFailedReasons.ShouldGenerateAnotherCode())
            {
                var pinGenerationResult = await _userVerificationService.GenerateEmailPin(Email!.PlainValue);

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

        var safeReturnUrl = !string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl) ?
            ReturnUrl :
            "/";

        if (user.EmailAddress != Email.PlainValue)
        {
            user.EmailAddress = Email.PlainValue;
            user.Updated = _clock.UtcNow;

            _dbContext.AddEvent(new Events.UserUpdatedEvent()
            {
                Source = Events.UserUpdatedEventSource.ChangedByUser,
                CreatedUtc = _clock.UtcNow,
                Changes = Events.UserUpdatedEventChanges.EmailAddress,
                User = Events.User.FromModel(user),
                UpdatedByUserId = User.GetUserId()!.Value,
                UpdatedByClientId = null
            });

            await _dbContext.SaveChangesAsync();

            if (HttpContext.TryGetAuthenticationState(out var authenticationState))
            {
                authenticationState.OnEmailChanged(Email!.PlainValue);

                // If we're inside an OAuth journey we need to redirect back to the authorize endpoint so the
                // OpenIddict auth handler can SignIn again with the revised user details

                authenticationState.EnsureOAuthState();
                Debug.Assert(ReturnUrl == _linkGenerator.CompleteAuthorization());

                safeReturnUrl = authenticationState.PostSignInUrl;
            }

            TempData.SetFlashSuccess("Email address updated");
        }

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
