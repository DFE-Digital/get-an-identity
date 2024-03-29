using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Infrastructure.Filters;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.Account.Phone;

[VerifyQueryParameterSignature]
public class Resend : BasePhonePageModel
{
    private readonly IdentityLinkGenerator _linkGenerator;

    public Resend(
        IUserVerificationService userVerificationService,
        IdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext) :
        base(userVerificationService, dbContext)
    {
        _linkGenerator = linkGenerator;
    }

    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();

    [BindProperty]
    [Display(Name = "Mobile number")]
    [Required(ErrorMessage = "Enter your new mobile phone number")]
    [MobilePhone(ErrorMessage = "Enter a valid mobile phone number")]
    public string? NewMobileNumber { get; set; }

    [FromQuery(Name = "mobileNumber")]
    public new string? MobileNumber { get; set; }

    public void OnGet()
    {
        NewMobileNumber = MobileNumber;
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var parsedMobileNumber = Models.MobileNumber.Parse(NewMobileNumber!);
        var existingUser = await FindUserByMobileNumber(parsedMobileNumber);

        if (existingUser is not null)
        {
            var errorMessage = existingUser.UserId == User.GetUserId()
                ? "Enter a different mobile phone number. The one you’ve entered is the same as the one already on your account"
                : "This mobile phone number is already in use - Enter a different mobile phone number";
            ModelState.AddModelError(nameof(NewMobileNumber), errorMessage);
            return this.PageWithErrors();
        }

        var smsPinGenerationResult = await GenerateSmsPinForNewPhone(parsedMobileNumber, nameof(NewMobileNumber));

        if (!smsPinGenerationResult.Success)
        {
            return smsPinGenerationResult.Result!;
        }

        return Redirect(_linkGenerator.AccountPhoneConfirm(MobileNumber!, ClientRedirectInfo));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (MobileNumber is null)
        {
            context.Result = BadRequest();
        }
    }
}
