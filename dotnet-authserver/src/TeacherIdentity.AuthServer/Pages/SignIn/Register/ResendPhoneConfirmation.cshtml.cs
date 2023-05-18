using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckJourneyType(typeof(CoreSignInJourney), typeof(CoreSignInJourneyWithTrnLookup), typeof(TrnTokenSignInJourney))]
[CheckCanAccessStep(CurrentStep)]
public class ResendPhoneConfirmationModel : BasePhonePageModel
{
    private const string CurrentStep = CoreSignInJourney.Steps.ResendPhoneConfirmation;

    private readonly SignInJourney _journey;

    public ResendPhoneConfirmationModel(
        IUserVerificationService userVerificationService,
        SignInJourney journey,
        TeacherIdentityServerDbContext dbContext) :
        base(userVerificationService, dbContext)
    {
        _journey = journey;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

    public void OnGet()
    {
        SetDefaultInputValues();
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var parsedMobileNumber = Models.MobileNumber.Parse(MobileNumber!);
        var pinGenerationResult = await GenerateSmsPinForNewPhone(parsedMobileNumber);

        if (!pinGenerationResult.Success)
        {
            return pinGenerationResult.Result!;
        }

        HttpContext.GetAuthenticationState().OnMobileNumberSet(MobileNumber!);

        return await _journey.Advance(CurrentStep);
    }

    private void SetDefaultInputValues()
    {
        MobileNumber ??= _journey.AuthenticationState.MobileNumber;
    }
}
