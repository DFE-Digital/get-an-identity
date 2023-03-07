using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

public class ResendConfirmExistingAccountEmail : BaseExistingEmailPageModel
{
    public ResendConfirmExistingAccountEmail(
        IUserVerificationService userVerificationService,
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext) :
        base(userVerificationService, linkGenerator, dbContext)
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var emailPinGenerationResult = await GenerateEmailPinForExistingEmail(HttpContext.GetAuthenticationState().ExistingAccountEmail!);

        if (!emailPinGenerationResult.Success)
        {
            return emailPinGenerationResult.Result!;
        }

        return Redirect(LinkGenerator.RegisterConfirmExistingAccount());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (HttpContext.GetAuthenticationState().ExistingAccountChosen != true)
        {
            context.Result = new RedirectResult(LinkGenerator.RegisterCheckAccount());
        }
    }
}
