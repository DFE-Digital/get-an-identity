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
    private readonly IEmailVerificationService _emailConfirmationService;

    public EmailConfirmationModel(
        TeacherIdentityServerDbContext dbContext,
        IEmailVerificationService emailConfirmationService,
        IIdentityLinkGenerator linkGenerator)
    {
        _dbContext = dbContext;
        _emailConfirmationService = emailConfirmationService;
        _linkGenerator = linkGenerator;
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

        var verifyPinFailedReasons = await _emailConfirmationService.VerifyPin(Email!, Code!);

        if (verifyPinFailedReasons != PinVerificationFailedReasons.None)
        {
            var generateAnotherCode = verifyPinFailedReasons.HasFlag(PinVerificationFailedReasons.ExpiredLessThanTwoHoursAgo);

            if (generateAnotherCode)
            {
                await _emailConfirmationService.GeneratePin(Email!);
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
        if (string.IsNullOrEmpty(Code))
        {
            ModelState.AddModelError(nameof(Code), "Enter a correct security code");
        }
        else if (!Code.All(c => c >= '0' && c <= '9'))
        {
            ModelState.AddModelError(nameof(Code), "The code must be 5 numbers");
        }
        else if (Code.Length < 5)
        {
            ModelState.AddModelError(nameof(Code), "You’ve not entered enough numbers, the code must be 5 numbers");
        }
        else if (Code.Length > 5)
        {
            ModelState.AddModelError(nameof(Code), "You’ve entered too many numbers, the code must be 5 numbers");
        }
    }
}
