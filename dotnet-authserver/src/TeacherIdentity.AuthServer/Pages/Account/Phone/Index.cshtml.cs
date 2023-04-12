using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.Account.Phone;

[BindProperties]
public class PhonePage : BasePhonePageModel
{
    private readonly IdentityLinkGenerator _linkGenerator;

    public PhonePage(
        IUserVerificationService userVerificationService,
        IdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext) :
        base(userVerificationService, dbContext)
    {
        _linkGenerator = linkGenerator;
    }

    [BindNever]
    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();

    [Display(Name = "Mobile number", Description = "For international numbers include the country code")]
    [Required(ErrorMessage = "Enter your new mobile phone number")]
    [MobilePhone(ErrorMessage = "Enter a valid mobile phone number")]
    public new string? MobileNumber { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var parsedMobileNumber = Models.MobileNumber.Parse(MobileNumber!);
        var existingUser = await FindUserByMobileNumber(parsedMobileNumber);

        if (existingUser is not null)
        {
            var errorMessage = existingUser.UserId == User.GetUserId()!.Value
                ? "Enter a different mobile phone number. The one you’ve entered is the same as the one already on your account"
                : "This mobile phone number is already in use - Enter a different mobile phone number";
            ModelState.AddModelError(nameof(MobileNumber), errorMessage);
            return this.PageWithErrors();
        }

        var smsPinGenerationResult = await GenerateSmsPinForNewPhone(parsedMobileNumber);

        if (!smsPinGenerationResult.Success)
        {
            return smsPinGenerationResult.Result!;
        }

        return Redirect(_linkGenerator.AccountPhoneConfirm(MobileNumber!, ClientRedirectInfo));
    }
}
