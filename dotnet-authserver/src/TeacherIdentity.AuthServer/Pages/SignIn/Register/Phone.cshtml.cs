using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

public class Phone : BasePhonePageModel
{
    private readonly IIdentityLinkGenerator _linkGenerator;

    public Phone(
        IUserVerificationService userVerificationService,
        IIdentityLinkGenerator linkGenerator) :
        base(userVerificationService)
    {
        _linkGenerator = linkGenerator;
    }

    public void OnGet()
    {
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
}
