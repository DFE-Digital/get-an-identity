using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[BindProperties]
public class EmailModel : BaseEmailPageModel
{
    public EmailModel(
        IEmailVerificationService emailVerificationService,
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext) :
        base(emailVerificationService, linkGenerator, dbContext)
    {
    }

    [Display(Name = "Email address", Description = "We’ll use this to send you a code to confirm your email address. Do not use a work or university email that you might lose access to.")]
    [Required(ErrorMessage = "Enter your email address")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string? Email { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var validateEmailResult = await TryValidateEmail(Email!);

        if (!validateEmailResult.IsValid)
        {
            return validateEmailResult.Result!;
        }

        HttpContext.GetAuthenticationState().OnEmailSet(Email!);

        return Redirect(LinkGenerator.RegisterEmailConfirmation());
    }
}
