using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

public class EmailConfirmationModel : BaseEmailConfirmationPageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IIdentityLinkGenerator _linkGenerator;

    public EmailConfirmationModel(
        TeacherIdentityServerDbContext dbContext,
        IEmailVerificationService emailConfirmationService,
        IIdentityLinkGenerator linkGenerator,
        PinValidator pinValidator)
        : base(emailConfirmationService, pinValidator)
    {
        _dbContext = dbContext;
        _linkGenerator = linkGenerator;
    }

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

        var verifyPinFailedReasons = await EmailVerificationService.VerifyPin(Email!, Code!);

        if (verifyPinFailedReasons != PinVerificationFailedReasons.None)
        {
            return await HandlePinVerificationFailed(verifyPinFailedReasons);
        }

        var authenticationState = HttpContext.GetAuthenticationState();
        var permittedUserTypes = authenticationState.GetPermittedUserTypes();

        var user = await _dbContext.Users.Where(u => u.EmailAddress == Email).SingleOrDefaultAsync();

        // If the UserType is not allowed then return an error
        if (user is not null && !permittedUserTypes.Contains(user.UserType))
        {
            return new ForbidResult();
        }

        // We only support registering users with the TRN requirement currently
        if (user is null && !authenticationState.UserRequirements.HasFlag(UserRequirements.TrnHolder))
        {
            return new ForbidResult();
        }

        authenticationState.OnEmailVerified(user);

        if (user is not null)
        {
            await authenticationState.SignIn(HttpContext);
        }

        return Redirect(authenticationState.GetNextHopUrl(_linkGenerator));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        // If email is already verified then move to the next page
        if (authenticationState.EmailAddressVerified)
        {
            context.Result = new RedirectResult(authenticationState.GetNextHopUrl(_linkGenerator));
        }
    }
}
