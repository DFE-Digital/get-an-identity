using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckJourneyType(typeof(CoreSignInJourney), typeof(CoreSignInJourneyWithTrnLookup))]
[CheckCanAccessStep(CurrentStep)]
public class AccountExists : BaseExistingEmailPageModel
{
    private const string CurrentStep = CoreSignInJourney.Steps.AccountExists;

    private readonly SignInJourney _journey;

    public AccountExists(
        SignInJourney journey,
        TeacherIdentityServerDbContext dbContext,
        IUserVerificationService userVerificationService,
        IClock clock, CreateUserHelper createUserHelper) :
        base(userVerificationService, dbContext)
    {
        _journey = journey;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

    public string? Email => HttpContext.GetAuthenticationState().EmailAddress;
    public string? ExistingAccountEmail => HttpContext.GetAuthenticationState().ExistingAccountEmail;

    [BindProperty]
    [Display(Name = " ")]
    [Required(ErrorMessage = "Select yes if this is your account")]
    public bool? IsUsersAccount { get; set; }

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost()
    {
        if (!ModelState.IsValid)
        {
            return this.PageWithErrors();
        }

        var authenticationState = HttpContext.GetAuthenticationState();
        authenticationState.OnExistingAccountChosen((bool)IsUsersAccount!);

        if (IsUsersAccount == true)
        {
            var emailPinGenerationResult = await GenerateEmailPinForExistingEmail(ExistingAccountEmail!);
            return emailPinGenerationResult.Success
                ? await _journey.Advance(CurrentStep)
                : emailPinGenerationResult.Result!;
        }

        return await _journey.Advance(CurrentStep);
    }
}
