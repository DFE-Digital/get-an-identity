using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckJourneyType(typeof(CoreSignInJourneyWithTrnLookup), typeof(CoreSignInJourney), typeof(TrnTokenSignInJourney))]
[CheckCanAccessStep(CurrentStep)]
public class InstitutionEmail : BaseEmailPageModel
{
    private const string CurrentStep = CoreSignInJourney.Steps.InstitutionEmail;

    private readonly SignInJourney _journey;

    public InstitutionEmail(
        IUserVerificationService userVerificationService,
        SignInJourney journey,
        TeacherIdentityServerDbContext dbContext) :
        base(userVerificationService, dbContext)
    {
        _journey = journey;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

    public string? InstitutionEmailAddress => _journey.AuthenticationState.EmailAddress;

    [BindProperty]
    [Display(Name = "Which email address would you like to use for your DfE Identity account?")]
    [Required(ErrorMessage = "Select which email address to use")]
    public bool? UsePersonalEmail { get; set; }

    [BindProperty]
    [Display(Name = "Email address")]
    [RequiredIfTrue(nameof(UsePersonalEmail), ErrorMessage = "Enter a personal email address")]
    [EmailAddressIfTrue(nameof(UsePersonalEmail), ErrorMessage = "Enter an email address in the correct format, like name@example.com")]
    public string? PersonalEmailAddress { get; set; }

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

        if (UsePersonalEmail == false)
        {
            HttpContext.GetAuthenticationState().OnInstitutionalEmailChosen();
        }
        else
        {
            var emailPinGenerationResult = await GenerateEmailPinForNewEmail(PersonalEmailAddress!, fieldName: "PersonalEmailAddress", allowInstitutionEmails: true);

            if (!emailPinGenerationResult.Success)
            {
                return emailPinGenerationResult.Result!;
            }

            _journey.AuthenticationState.OnEmailSet(PersonalEmailAddress!, await IsInstitutionEmail(PersonalEmailAddress!));
        }

        return await _journey.Advance(CurrentStep);
    }

    private void SetDefaultInputValues()
    {
        UsePersonalEmail ??= !_journey.AuthenticationState.InstitutionEmailChosen;
    }
}
