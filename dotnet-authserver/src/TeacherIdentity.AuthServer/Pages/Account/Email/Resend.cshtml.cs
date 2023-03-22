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

    [BindProperty]
    [Display(Name = "Email address")]
    [Required(ErrorMessage = "Enter your new email address")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string? NewEmail { get; set; }

    [FromQuery(Name = "email")]
    public ProtectedString? Email { get; set; }

    [FromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

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

        if (await EmailExists(NewEmail!))
        {
            ModelState.AddModelError(nameof(NewEmail), "This email address is already in use - Enter a different email address");
            return this.PageWithErrors();
        }

        var emailPinGenerationResult = await GenerateEmailPinForNewEmail(NewEmail!, nameof(NewEmail));

        if (!emailPinGenerationResult.Success)
        {
            return emailPinGenerationResult.Result!;
        }

        var protectedEmail = _protectedStringFactory.CreateFromPlainValue(NewEmail!);

        return Redirect(LinkGenerator.AccountEmailConfirm(protectedEmail, ReturnUrl));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (Email is null)
        {
            context.Result = new BadRequestResult();
        }
    }
}
