using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[RequireAuthenticationMilestone(AuthenticationState.AuthenticationMilestone.None)]
public class EmailConfirmationModel : BaseEmailConfirmationPageModel
{
    private readonly TeacherIdentityServerDbContext _dbContext;
    private readonly IIdentityLinkGenerator _linkGenerator;

    public EmailConfirmationModel(
        TeacherIdentityServerDbContext dbContext,
        IUserVerificationService userVerificationService,
        IIdentityLinkGenerator linkGenerator,
        PinValidator pinValidator)
        : base(userVerificationService, pinValidator)
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

        var verifyEmailPinFailedReasons = await UserVerificationService.VerifyEmailPin(Email!, Code!);

        if (verifyEmailPinFailedReasons != PinVerificationFailedReasons.None)
        {
            return await HandlePinVerificationFailed(verifyEmailPinFailedReasons);
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

        if (!authenticationState.EmailAddressSet)
        {
            context.Result = new RedirectResult(_linkGenerator.Email());
        }
    }
}
