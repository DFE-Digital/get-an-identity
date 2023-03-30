using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[RequireAuthenticationMilestone(AuthenticationState.AuthenticationMilestone.TrnLookupCompleted)]
public class TrnInUseModel : BaseEmailConfirmationPageModel
{
    private readonly IdentityLinkGenerator _linkGenerator;

    public TrnInUseModel(
        IUserVerificationService userVerificationService,
        PinValidator pinValidator,
        IdentityLinkGenerator linkGenerator)
        : base(userVerificationService, pinValidator)
    {
        _linkGenerator = linkGenerator;
    }

    public override string Email => HttpContext.GetAuthenticationState().TrnOwnerEmailAddress!;

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

        var VerifyEmailPinFailedReasons = await UserVerificationService.VerifyEmailPin(Email!, Code!);

        if (VerifyEmailPinFailedReasons != PinVerificationFailedReasons.None)
        {
            return await HandlePinVerificationFailed(VerifyEmailPinFailedReasons);
        }

        var authenticationState = HttpContext.GetAuthenticationState();
        authenticationState.OnEmailVerifiedOfExistingAccountForTrn();

        return Redirect(authenticationState.GetNextHopUrl(_linkGenerator));
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = HttpContext.GetAuthenticationState();

        if (authenticationState.TrnLookup != AuthenticationState.TrnLookupState.ExistingTrnFound)
        {
            context.Result = Redirect(authenticationState.GetNextHopUrl(_linkGenerator));
        }
    }
}
