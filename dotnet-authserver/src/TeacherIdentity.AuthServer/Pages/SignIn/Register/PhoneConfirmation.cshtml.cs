using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

public class PhoneConfirmation : BasePhoneConfirmationPageModel
{
    private readonly IdentityLinkGenerator _linkGenerator;
    private readonly TeacherIdentityServerDbContext _dbContext;

    public PhoneConfirmation(
        IUserVerificationService userVerificationService,
        PinValidator pinValidator,
        IdentityLinkGenerator linkGenerator,
        TeacherIdentityServerDbContext dbContext)
        : base(userVerificationService, pinValidator)
    {
        _linkGenerator = linkGenerator;
        _dbContext = dbContext;
    }

    [BindProperty]
    [Display(Name = "Security code")]
    public override string? Code { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        Code = Code?.Trim();
        ValidateCode();

        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var parsedMobileNumber = Models.MobileNumber.Parse(MobileNumber!);
        var pinVerificationFailedReasons = await UserVerificationService.VerifySmsPin(parsedMobileNumber, Code!);

        if (pinVerificationFailedReasons != PinVerificationFailedReasons.None)
        {
            return await HandlePinVerificationFailed(pinVerificationFailedReasons);
        }

        var authenticationState = HttpContext.GetAuthenticationState();
        var permittedUserTypes = authenticationState.GetPermittedUserTypes();

        var user = await _dbContext.Users
            .Where(u => u.NormalizedMobileNumber! == parsedMobileNumber)
            .SingleOrDefaultAsync();

        if (user is not null && !permittedUserTypes.Contains(user.UserType))
        {
            return new ForbidResult();
        }

        authenticationState.OnMobileNumberVerified(user);

        if (user is not null)
        {
            await authenticationState.SignIn(HttpContext);
            return Redirect(_linkGenerator.RegisterPhoneExists());
        }

        return Redirect(_linkGenerator.RegisterName());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (!authenticationState.MobileNumberSet)
        {
            context.Result = new RedirectResult(_linkGenerator.RegisterPhone());
        }
    }
}
