using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

public class Phone : BasePhonePageModel
{
    private readonly IdentityLinkGenerator _linkGenerator;

    public Phone(
        IUserVerificationService userVerificationService,
        IdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext) :
        base(userVerificationService, dbContext)
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

        var parsedMobileNumber = Models.MobileNumber.Parse(MobileNumber!);
        var pinGenerationResult = await GenerateSmsPinForNewPhone(parsedMobileNumber);

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

        if (!authenticationState.EmailAddressVerified)
        {
            context.Result = new RedirectResult(_linkGenerator.RegisterEmailConfirmation());
        }
    }
}
