using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

public class EmailConfirmationModel : BaseEmailConfirmationPageModel
{
    private readonly IIdentityLinkGenerator _linkGenerator;

    public EmailConfirmationModel(
        IEmailVerificationService emailConfirmationService,
        PinValidator pinValidator,
        IIdentityLinkGenerator linkGenerator)
        : base(emailConfirmationService, pinValidator)
    {
        _linkGenerator = linkGenerator;
    }

    [BindProperty]
    [Display(Name = "Confirmation code")]
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

        var pinVerificationFailedReasons = await EmailVerificationService.VerifyPin(Email!, Code!);

        if (pinVerificationFailedReasons != PinVerificationFailedReasons.None)
        {
            return await HandlePinVerificationFailed(pinVerificationFailedReasons);
        }

        HttpContext.GetAuthenticationState().OnEmailVerified();

        return Redirect(_linkGenerator.RegisterName());
    }
}
