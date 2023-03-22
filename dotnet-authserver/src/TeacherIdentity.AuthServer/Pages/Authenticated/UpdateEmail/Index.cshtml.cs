using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.Authenticated.UpdateEmail;

public class IndexModel : BaseEmailPageModel
{
    private readonly ProtectedStringFactory _protectedStringFactory;

    public IndexModel(
        TeacherIdentityServerDbContext dbContext,
        IUserVerificationService userVerificationService,
        ProtectedStringFactory protectedStringFactory,
        IIdentityLinkGenerator linkGenerator)
        : base(userVerificationService, linkGenerator, dbContext)
    {
        _protectedStringFactory = protectedStringFactory;
    }

    [BindProperty]
    [Display(Name = "Enter your new email address")]
    [Required(ErrorMessage = "Enter your new email address")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string? Email { get; set; }

    [FromQuery(Name = "cancelUrl")]
    public string? CancelUrl { get; set; }

    [FromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

    public void OnGet()
    {
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
            var errorMessage = existingUser.UserId == User.GetUserId()!.Value
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

        var protectedEmail = _protectedStringFactory.CreateFromPlainValue(Email!);

        return Redirect(LinkGenerator.UpdateEmailConfirmation(protectedEmail, ReturnUrl, CancelUrl));
    }
}
