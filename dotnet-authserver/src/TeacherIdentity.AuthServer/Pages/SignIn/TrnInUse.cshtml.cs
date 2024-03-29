using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[CheckCanAccessStep(CurrentStep)]
public class TrnInUseModel : BaseEmailConfirmationPageModel
{
    private const string CurrentStep = SignInJourney.Steps.TrnInUse;

    private readonly SignInJourney _journey;

    public TrnInUseModel(
        SignInJourney journey,
        IUserVerificationService userVerificationService,
        PinValidator pinValidator)
        : base(userVerificationService, pinValidator)
    {
        _journey = journey;
    }

    public override string Email => _journey.AuthenticationState.TrnOwnerEmailAddress!;

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

        _journey.AuthenticationState.OnEmailVerifiedOfExistingAccountForTrn();

        return Redirect(_journey.GetNextStepUrl(CurrentStep));
    }
}
