using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[RequireAuthenticationMilestone(AuthenticationState.AuthenticationMilestone.None)]
public class EmailModel : BaseEmailPageModel
{
    public EmailModel(
        IUserVerificationService userVerificationService,
        IdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext) :
        base(userVerificationService, linkGenerator, dbContext)
    {
    }

    [BindProperty]
    [Display(Name = "Your email address", Description = "Enter the email you used when creating your DfE Identity account.")]
    [Required(ErrorMessage = "Enter your email address")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string? Email { get; set; }

    public AuthenticationJourneyType JourneyType => HttpContext.GetAuthenticationState().GetJourneyType();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var emailPinGenerationResult = await GenerateEmailPinForNewEmail(Email!);

        if (!emailPinGenerationResult.Success)
        {
            return emailPinGenerationResult.Result!;
        }

        HttpContext.GetAuthenticationState().OnEmailSet(Email!);

        return Redirect(LinkGenerator.EmailConfirmation());
    }
}
