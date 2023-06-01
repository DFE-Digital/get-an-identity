using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.Account.Email;

[BindProperties]
public class EmailPage : BaseEmailPageModel
{
    private IdentityLinkGenerator _linkGenerator;

    public EmailPage(
        IUserVerificationService userVerificationService,
        IdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext) :
        base(userVerificationService, dbContext)
    {
        _linkGenerator = linkGenerator;
    }

    [BindNever]
    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();

    [Display(Name = "Email address", Description = "We’ll use this to send you a code to confirm your email address. Do not use a work or university email that you might lose access to.")]
    [Required(ErrorMessage = "Enter your new email address")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string? Email { get; set; }

    public void OnGet()
    {
        Email = User.GetEmailAddress();
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var existingUser = await FindUserByEmailAddress(Email!);

        if (existingUser is not null)
        {
            var errorMessage = existingUser.UserId == User.GetUserId()
                ? "Enter a different email address. The one you’ve entered is the same as the one already on your account"
                : "This email address is already in use - Enter a different email address";
            ModelState.AddModelError(nameof(Email), errorMessage);
            return this.PageWithErrors();
        }

        var emailPinGenerationResult = await GenerateEmailPinForNewEmail(Email!);

        if (!emailPinGenerationResult.Success)
        {
            return emailPinGenerationResult.Result!;
        }

        return Redirect(_linkGenerator.AccountEmailConfirm(Email!, ClientRedirectInfo));
    }
}
