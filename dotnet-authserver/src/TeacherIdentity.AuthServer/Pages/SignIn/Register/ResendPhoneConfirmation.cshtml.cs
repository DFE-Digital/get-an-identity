using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

public class ResendPhoneConfirmationModel : BasePhonePageModel
{
    private IIdentityLinkGenerator _linkGenerator;

    public ResendPhoneConfirmationModel(
        IUserVerificationService userVerificationService,
        IIdentityLinkGenerator linkGenerator) :
        base(userVerificationService)
    {
        _linkGenerator = linkGenerator;
    }

    public void OnGet()
    {
        MobileNumber = HttpContext.GetAuthenticationState().MobileNumber;
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var pinGenerationResult = await GenerateSmsPinForNewPhone(MobileNumber!);

        if (!pinGenerationResult.Success)
        {
            return pinGenerationResult.Result!;
        }

        HttpContext.GetAuthenticationState().OnMobileNumberSet(MobileNumber!);

        return Redirect(_linkGenerator.RegisterPhoneConfirmation());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (authenticationState.MobileNumber is null)
        {
            context.Result = Redirect(_linkGenerator.RegisterPhone());
        }
        else if (authenticationState.MobileNumberVerified)
        {
            context.Result = Redirect(_linkGenerator.RegisterName());
        }
    }
}
