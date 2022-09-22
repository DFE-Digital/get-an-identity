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
            if (verifyPinFailedReasons.ShouldGenerateAnotherCode())
            {
                await _emailVerificationService.GeneratePin(Email!);
                ModelState.AddModelError(nameof(Code), "The security code has expired. New code sent.");
            }
            else
            {
                ModelState.AddModelError(nameof(Code), "Enter a correct security code");
            }

            return this.PageWithErrors();
        }

        var authenticationState = HttpContext.GetAuthenticationState();
        var requiredUserType = authenticationState.GetUserType();

        var user = await _dbContext.Users.Where(u => u.EmailAddress == Email).SingleOrDefaultAsync();

        // If we the UserType is not what we expect, return an error
        if (user is not null && user.UserType != requiredUserType)
        {
            return new ForbidResult(authenticationScheme: CookieAuthenticationDefaults.AuthenticationScheme);
        }

        authenticationState.OnEmailVerified(user);

        if (user is not null)
        {
            await HttpContext.SignInUserFromAuthenticationState();
        }

        if (requiredUserType == UserType.Staff && user is null)
        {
            // We don't support registering staff users
            return new ForbidResult(authenticationScheme: CookieAuthenticationDefaults.AuthenticationScheme);
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
