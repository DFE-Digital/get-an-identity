using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[BindProperties]
[RequireAuthenticationMilestone(AuthenticationState.AuthenticationMilestone.None)]
public class EmailModel : BaseEmailPageModel
{
    public EmailModel(
        IEmailVerificationService emailVerificationService,
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext) :
        base(emailVerificationService, linkGenerator, dbContext)
    {
    }

    [Display(Name = "Enter your email address", Description = "Use your personal email address. This is so you can keep these sign in details should you change jobs.")]
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

        return Redirect(LinkGenerator.EmailConfirmation());
    }
}
