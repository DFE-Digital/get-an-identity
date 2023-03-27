using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.Account.Phone;

public class Resend : BasePhonePageModel
{
    private ProtectedStringFactory _protectedStringFactory;
    private IIdentityLinkGenerator _linkGenerator;

    public Resend(
        IUserVerificationService userVerificationService,
        IIdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext,
        ProtectedStringFactory protectedStringFactory) :
        base(userVerificationService, dbContext)
    {
        _linkGenerator = linkGenerator;
        _protectedStringFactory = protectedStringFactory;
    }

    public ClientRedirectInfo? ClientRedirectInfo => HttpContext.GetClientRedirectInfo();

    [BindProperty]
    [Display(Name = "Mobile number")]
    [Required(ErrorMessage = "Enter your new mobile phone number")]
    [Phone(ErrorMessage = "Enter a valid mobile phone number")]
    public string? NewMobileNumber { get; set; }

    [FromQuery(Name = "mobileNumber")]
    public new ProtectedString? MobileNumber { get; set; }

    public void OnGet()
    {
        NewMobileNumber = MobileNumber!.PlainValue;
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var existingUser = await FindUserByMobileNumber(NewMobileNumber!);

        if (existingUser is not null)
        {
            var errorMessage = existingUser.UserId == User.GetUserId()!.Value
                ? "Enter a different mobile phone number. The one you’ve entered is the same as the one already on your account"
                : "This mobile phone number is already in use - Enter a different mobile phone number";
            ModelState.AddModelError(nameof(NewMobileNumber), errorMessage);
            return this.PageWithErrors();
        }

        var smsPinGenerationResult = await GenerateSmsPinForNewPhone(NewMobileNumber!, nameof(NewMobileNumber));

        if (!smsPinGenerationResult.Success)
        {
            return smsPinGenerationResult.Result!;
        }

        var protectedMobileNumber = _protectedStringFactory.CreateFromPlainValue(NewMobileNumber!);

        return Redirect(_linkGenerator.AccountPhoneConfirm(protectedMobileNumber, ClientRedirectInfo));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (MobileNumber is null)
        {
            context.Result = BadRequest();
        }
    }
}
