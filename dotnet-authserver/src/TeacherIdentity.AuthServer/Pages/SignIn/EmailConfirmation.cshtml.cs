using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class EmailConfirmationModel : PageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IIdentityLinkGenerator _linkGenerator;
    private readonly PinValidator _pinValidator;
    private readonly IEmailVerificationService _emailVerificationService;

    public EmailConfirmationModel(
        TeacherIdentityServerDbContext dbContext,
        IEmailVerificationService emailConfirmationService,
        IIdentityLinkGenerator linkGenerator,
        PinValidator pinValidator)
    {
        _dbContext = dbContext;
        _emailVerificationService = emailConfirmationService;
        _linkGenerator = linkGenerator;
        _pinValidator = pinValidator;
    }

    public string? Email => HttpContext.GetAuthenticationState().EmailAddress;

    [BindProperty]
    [Display(Name = "Enter your code")]
    public string? Code { get; set; }

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

        var verifyPinFailedReasons = await _emailVerificationService.VerifyPin(Email!, Code!);

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

                ModelState.AddModelError(nameof(Code), "The security code has expired. New code sent.");
            }
            else
            {
                ModelState.AddModelError(nameof(Code), "Enter a correct security code");
            }

            return this.PageWithErrors();
        }

        var authenticationState = HttpContext.GetAuthenticationState();
        var permittedUserTypes = authenticationState.GetPermittedUserTypes();

        var user = await _dbContext.Users.Where(u => u.EmailAddress == Email).SingleOrDefaultAsync();

        // If the UserType is not allowed then return an error
        if (user is not null && !permittedUserTypes.Contains(user.UserType))
        {
            return new ForbidResult(authenticationScheme: CookieAuthenticationDefaults.AuthenticationScheme);
        }

        // We only support registering users with the TRN requirement currently
        if (user is null && !authenticationState.UserRequirements.HasFlag(UserRequirements.TrnHolder))
        {
            return new ForbidResult(authenticationScheme: CookieAuthenticationDefaults.AuthenticationScheme);
        }

        authenticationState.OnEmailVerified(user);

        if (user is not null)
        {
            await authenticationState.SignIn(HttpContext);
        }

        return Redirect(authenticationState.GetNextHopUrl(_linkGenerator));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        // If email is already verified then move to the next page
        if (authenticationState.EmailAddressVerified)
        {
            context.Result = new RedirectResult(authenticationState.GetNextHopUrl(_linkGenerator));
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
