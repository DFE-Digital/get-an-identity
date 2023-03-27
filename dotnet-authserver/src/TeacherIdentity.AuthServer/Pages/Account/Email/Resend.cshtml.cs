using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.Account.Email;

public class Resend : BaseEmailPageModel
{
    private ProtectedStringFactory _protectedStringFactory;

    public Resend(
        IUserVerificationService userVerificationService,
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext,
        ProtectedStringFactory protectedStringFactory) :
        base(userVerificationService, linkGenerator, dbContext)
    {
        _protectedStringFactory = protectedStringFactory;
    }

    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();

    [BindProperty]
    [Display(Name = "Email address")]
    [Required(ErrorMessage = "Enter your new email address")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string? NewEmail { get; set; }

    [FromQuery(Name = "email")]
    public ProtectedString? Email { get; set; }

    public void OnGet()
    {
        NewEmail = Email!.PlainValue;
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var existingUser = await FindUserByEmailAddress(NewEmail!);

        if (existingUser is not null)
        {
            var errorMessage = existingUser.UserId == User.GetUserId()!.Value
                ? "Enter a different email address. The one you’ve entered is the same as the one already on your account"
                : "This email address is already in use - Enter a different email address";
            ModelState.AddModelError(nameof(NewEmail), errorMessage);
            return this.PageWithErrors();
        }

        var emailPinGenerationResult = await GenerateEmailPinForNewEmail(NewEmail!, nameof(NewEmail));

        if (!emailPinGenerationResult.Success)
        {
            return emailPinGenerationResult.Result!;
        }

        var protectedEmail = _protectedStringFactory.CreateFromPlainValue(NewEmail!);

        return Redirect(LinkGenerator.AccountEmailConfirm(protectedEmail, ClientRedirectInfo));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (Email is null)
        {
            context.Result = BadRequest();
        }
    }
}
