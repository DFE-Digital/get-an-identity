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

    [BindProperty]
    [Display(Name = "Mobile number")]
    [Required(ErrorMessage = "Enter your new mobile phone number")]
    [Phone(ErrorMessage = "Enter a valid mobile phone number")]
    public string? NewMobileNumber { get; set; }

    [FromQuery(Name = "mobileNumber")]
    public new ProtectedString? MobileNumber { get; set; }

    [FromQuery(Name = "returnUrl")]
    public string? ReturnUrl { get; set; }

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

        if (await MobileNumberExists(NewMobileNumber!))
        {
            ModelState.AddModelError(nameof(NewMobileNumber), "This mobile phone number is already in use - Enter a different mobile phone number");
            return this.PageWithErrors();
        }

        var smsPinGenerationResult = await GenerateSmsPinForNewPhone(NewMobileNumber!, nameof(NewMobileNumber));

        if (!smsPinGenerationResult.Success)
        {
            return smsPinGenerationResult.Result!;
        }

        var protectedMobileNumber = _protectedStringFactory.CreateFromPlainValue(NewMobileNumber!);

        return Redirect(_linkGenerator.AccountPhoneConfirm(protectedMobileNumber, ReturnUrl));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (MobileNumber is null)
        {
            context.Result = new BadRequestResult();
        }
    }
}
