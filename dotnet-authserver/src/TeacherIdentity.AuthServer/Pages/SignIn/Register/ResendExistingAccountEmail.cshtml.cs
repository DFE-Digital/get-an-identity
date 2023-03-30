using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

public class ResendExistingAccountEmail : BaseExistingEmailPageModel
{
    public ResendExistingAccountEmail(
        IUserVerificationService userVerificationService,
        IdentityLinkGenerator linkGenerator,
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

        return Redirect(LinkGenerator.RegisterExistingAccountEmailConfirmation());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (authenticationState.ExistingAccountChosen != true)
        {
            context.Result = Redirect(LinkGenerator.RegisterAccountExists());
        }
    }
}
