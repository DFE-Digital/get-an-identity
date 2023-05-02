using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckCanAccessStep(CurrentStep)]
public class ResendExistingAccountPhone : BaseExistingPhonePageModel
{
    private const string CurrentStep = CoreSignInJourney.Steps.ResendExistingAccountPhone;

    private readonly SignInJourney _journey;

    public ResendExistingAccountPhone(
        IUserVerificationService userVerificationService,
        SignInJourney journey) :
        base(userVerificationService)
    {
        _journey = journey;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

    public string? ExistingMobileNumber => HttpContext.GetAuthenticationState().ExistingAccountMobileNumber;

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var parsedMobileNumber = Models.MobileNumber.Parse(ExistingMobileNumber!);
        var pinGenerationResult = await GenerateSmsPinForExistingMobileNumber(parsedMobileNumber);

        if (!pinGenerationResult.Success)
        {
            return pinGenerationResult.Result!;
        }

        return await _journey.Advance(CurrentStep);
    }
}
