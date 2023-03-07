using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

public class SendConfirmExistingAccountPhone : BaseExistingPhonePageModel
{
    private IIdentityLinkGenerator _linkGenerator;

    public SendConfirmExistingAccountPhone(
        IUserVerificationService userVerificationService,
        IIdentityLinkGenerator linkGenerator) :
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

        return Redirect(_linkGenerator.RegisterConfirmExistingAccountPhone());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (HttpContext.GetAuthenticationState().ExistingAccountMobileNumber is null ||
            HttpContext.GetAuthenticationState().ExistingAccountChosen != true)
        {
            context.Result = new RedirectResult(_linkGenerator.RegisterCheckAccount());
        }
    }
}
