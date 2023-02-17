using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

public class EmailConfirmationModel : BaseEmailConfirmationPageModel
{
    private readonly IIdentityLinkGenerator _linkGenerator;

    public EmailConfirmationModel(
        IUserVerificationService userVerificationService,
        PinValidator pinValidator,
        IIdentityLinkGenerator linkGenerator)
        : base(userVerificationService, pinValidator)
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

        var pinVerificationFailedReasons = await UserVerificationService.VerifyEmailPin(Email!, Code!);

        if (pinVerificationFailedReasons != PinVerificationFailedReasons.None)
        {
            return await HandlePinVerificationFailed(pinVerificationFailedReasons);
        }

        HttpContext.GetAuthenticationState().OnEmailVerified();

        return Redirect(_linkGenerator.RegisterPhone());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (!authenticationState.EmailAddressSet)
        {
            context.Result = new RedirectResult(_linkGenerator.RegisterEmail());
        }
    }
}
