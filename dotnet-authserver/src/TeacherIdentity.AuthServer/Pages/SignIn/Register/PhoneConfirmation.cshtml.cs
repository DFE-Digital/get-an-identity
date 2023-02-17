using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

public class PhoneConfirmation : BasePhoneConfirmationPageModel
{
    private readonly IIdentityLinkGenerator _linkGenerator;
    private readonly TeacherIdentityServerDbContext _dbContext;

    public PhoneConfirmation(
        IUserVerificationService userVerificationService,
        PinValidator pinValidator,
        IIdentityLinkGenerator linkGenerator,
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

        var pinVerificationFailedReasons = await UserVerificationService.VerifySmsPin(MobileNumber!, Code!);

        if (pinVerificationFailedReasons != PinVerificationFailedReasons.None)
        {
            return await HandlePinVerificationFailed(pinVerificationFailedReasons);
        }

        var authenticationState = HttpContext.GetAuthenticationState();
        var permittedUserTypes = authenticationState.GetPermittedUserTypes();

        var user = await _dbContext.Users
            .Where(u => (u.MobileNumber ?? "").EndsWith(NormaliseMobileNumber(MobileNumber!)))
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

    private string NormaliseMobileNumber(string mobileNumber)
    {
        return new string(RemoveUkCountryCode(mobileNumber).Where(char.IsDigit).ToArray()).TrimStart('0');
    }

    private string RemoveUkCountryCode(string mobileNumber)
    {
        return Regex.Replace(mobileNumber, @"^(\+44|0044)", "");
    }
}
