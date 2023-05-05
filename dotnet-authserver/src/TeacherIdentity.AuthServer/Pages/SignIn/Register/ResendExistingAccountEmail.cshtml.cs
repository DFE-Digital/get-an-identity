using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckJourneyType(typeof(CoreSignInJourney), typeof(CoreSignInJourneyWithTrnLookup))]
[CheckCanAccessStep(CurrentStep)]
public class ResendExistingAccountEmail : BaseExistingEmailPageModel
{
    private const string CurrentStep = CoreSignInJourney.Steps.ResendExistingAccountEmail;

    private readonly SignInJourney _journey;

    public ResendExistingAccountEmail(
        IUserVerificationService userVerificationService,
        SignInJourney journey,
        TeacherIdentityServerDbContext dbContext) :
        base(userVerificationService, dbContext)
    {
        _journey = journey;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var emailPinGenerationResult = await GenerateEmailPinForExistingEmail(HttpContext.GetAuthenticationState().ExistingAccountEmail!);

        if (!emailPinGenerationResult.Success)
        {
            return emailPinGenerationResult.Result!;
        }

        return await _journey.Advance(CurrentStep);
    }
}
