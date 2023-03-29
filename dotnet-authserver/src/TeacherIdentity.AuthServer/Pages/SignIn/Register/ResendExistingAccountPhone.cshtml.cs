using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

public class ResendExistingAccountPhone : BaseExistingPhonePageModel
{
    private IdentityLinkGenerator _linkGenerator;

    public ResendExistingAccountPhone(
        IUserVerificationService userVerificationService,
        IdentityLinkGenerator linkGenerator) :
        base(userVerificationService)
    {
        _linkGenerator = linkGenerator;
    }

    public string? ExistingMobileNumber => HttpContext.GetAuthenticationState().ExistingAccountMobileNumber;

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var pinGenerationResult = await GenerateSmsPinForExistingMobileNumber(ExistingMobileNumber!);

        if (!pinGenerationResult.Success)
        {
            return pinGenerationResult.Result!;
        }

        return Redirect(_linkGenerator.RegisterExistingAccountPhoneConfirmation());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (authenticationState.ExistingAccountChosen != true)
        {
            context.Result = Redirect(_linkGenerator.RegisterAccountExists());
            return;
        }

        if (authenticationState.ExistingAccountMobileNumber is null)
        {
            context.Result = Redirect(_linkGenerator.RegisterExistingAccountEmailConfirmation());
        }
    }
}
