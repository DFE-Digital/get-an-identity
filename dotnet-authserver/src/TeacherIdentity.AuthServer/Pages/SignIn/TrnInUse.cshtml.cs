using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Services.EmailVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[RequireAuthenticationMilestone(AuthenticationState.AuthenticationMilestone.TrnLookupCompleted)]
public class TrnInUseModel : BaseEmailConfirmationPageModel
{
    private readonly IIdentityLinkGenerator _linkGenerator;

    public TrnInUseModel(
        IEmailVerificationService emailVerificationService,
        PinValidator pinValidator,
        IIdentityLinkGenerator linkGenerator)
        : base(emailVerificationService, pinValidator)
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

        var verifyPinFailedReasons = await EmailVerificationService.VerifyPin(Email!, Code!);

        if (verifyPinFailedReasons != PinVerificationFailedReasons.None)
        {
            return await HandlePinVerificationFailed(verifyPinFailedReasons);
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
