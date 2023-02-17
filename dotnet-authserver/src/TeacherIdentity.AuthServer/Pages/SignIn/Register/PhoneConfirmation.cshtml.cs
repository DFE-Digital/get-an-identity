using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

public class PhoneConfirmation : BasePhoneConfirmationPageModel
{
    private readonly IIdentityLinkGenerator _linkGenerator;

    public PhoneConfirmation(
        IUserVerificationService userConfirmationService,
        PinValidator pinValidator,
        IIdentityLinkGenerator linkGenerator)
        : base(userConfirmationService, pinValidator)
    {
        _linkGenerator = linkGenerator;
    }

    [BindProperty]
    [Display(Name = "Security code")]
    public override string? Code { get; set; }

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

        var pinVerificationFailedReasons = await UserVerificationService.VerifySmsPin(MobileNumber!, Code!);

        if (pinVerificationFailedReasons != PinVerificationFailedReasons.None)
        {
            return await HandlePinVerificationFailed(pinVerificationFailedReasons);
        }

        HttpContext.GetAuthenticationState().OnMobileNumberVerified();

        return Redirect(_linkGenerator.RegisterName());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (!authenticationState.MobileNumberSet)
        {
            context.Result = new RedirectResult(_linkGenerator.RegisterPhone());
        }
    }
}
