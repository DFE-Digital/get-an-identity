using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[BindProperties]
[RequireAuthenticationMilestone(AuthenticationState.AuthenticationMilestone.None)]
public class ResendEmailConfirmationModel : BaseEmailPageModel
{
    public ResendEmailConfirmationModel(
        IEmailVerificationService emailVerificationService,
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext) :
        base(emailVerificationService, linkGenerator, dbContext)
    {
    }

    [Display(Name = "Email address")]
    [Required(ErrorMessage = "Enter your email address")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string? Email { get; set; }

    public void OnGet()
    {
        Email = HttpContext.GetAuthenticationState().EmailAddress;
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var validateEmailResult = await ValidateEmail(Email!);

        if (!validateEmailResult.IsValid)
        {
            return validateEmailResult.Result!;
        }

        HttpContext.GetAuthenticationState().OnEmailSet(Email!);

        return Redirect(LinkGenerator.EmailConfirmation());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (authenticationState.EmailAddress is null)
        {
            context.Result = Redirect(LinkGenerator.Email());
        }
        else if (authenticationState.EmailAddressVerified)
        {
            context.Result = Redirect(authenticationState.GetNextHopUrl(LinkGenerator));
        }
    }
}
