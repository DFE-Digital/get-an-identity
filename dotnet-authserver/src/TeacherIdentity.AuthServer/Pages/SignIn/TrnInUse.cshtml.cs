using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class TrnInUseModel : PageModel
{
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly PinValidator _pinValidator;
    private readonly IIdentityLinkGenerator _linkGenerator;

    public TrnInUseModel(
        IEmailVerificationService emailVerificationService,
        PinValidator pinValidator,
        IIdentityLinkGenerator linkGenerator)
    {
        _emailVerificationService = emailVerificationService;
        _pinValidator = pinValidator;
        _linkGenerator = linkGenerator;
    }

    public string Email => HttpContext.GetAuthenticationState().TrnOwnerEmailAddress!;

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
        authenticationState.OnEmailVerifiedOfExistingAccountForTrn();

        return Redirect(authenticationState.GetNextHopUrl(_linkGenerator));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = HttpContext.GetAuthenticationState();

        if (authenticationState.TrnLookup != AuthenticationState.TrnLookupState.ExistingTrnFound)
        {
            context.Result = Redirect(authenticationState.GetNextHopUrl(_linkGenerator));
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
