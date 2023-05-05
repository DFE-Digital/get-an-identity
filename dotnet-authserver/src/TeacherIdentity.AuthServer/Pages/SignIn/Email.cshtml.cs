using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[CheckCanAccessStep(CurrentStep)]
public class EmailModel : BaseEmailPageModel
{
    private const string CurrentStep = SignInJourney.Steps.Email;

    private readonly SignInJourney _journey;

    public EmailModel(
        SignInJourney journey,
        IUserVerificationService userVerificationService,
        TeacherIdentityServerDbContext dbContext) :
        base(userVerificationService, dbContext)
    {
        _journey = journey;
    }

    public string? BackLink => _journey.TryGetPreviousStepUrl(CurrentStep, out var stepUrl) ? stepUrl : null;

    [BindProperty]
    [Display(Name = "Your email address", Description = "Enter the email you used when creating your DfE Identity account.")]
    [Required(ErrorMessage = "Enter your email address")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string? Email { get; set; }

    public Type JourneyType => _journey.GetType();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var emailPinGenerationResult = await GenerateEmailPinForNewEmail(Email!);

        if (!emailPinGenerationResult.Success)
        {
            return emailPinGenerationResult.Result!;
        }

        _journey.AuthenticationState.OnEmailSet(Email!);

        return Redirect(_journey.GetNextStepUrl(CurrentStep));
    }
}
