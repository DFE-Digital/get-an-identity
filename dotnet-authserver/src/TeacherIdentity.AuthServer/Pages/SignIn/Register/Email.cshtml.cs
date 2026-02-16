using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckJourneyType(typeof(CoreSignInJourney), typeof(CoreSignInJourneyWithTrnLookup), typeof(TrnTokenSignInJourney))]
[CheckCanAccessStep(CurrentStep)]
public class EmailModel : BaseEmailPageModel
{
    private const string CurrentStep = CoreSignInJourney.Steps.Email;

    private readonly SignInJourney _journey;

    private readonly ICurrentClientProvider _currentClientProvider;
    public PreventRegistrationOptions PreventRegistrationOptions { get; init; }
    public Application? CurrentClient { get; private set; }

    public EmailModel(
        IUserVerificationService userVerificationService,
        SignInJourney journey,
        TeacherIdentityServerDbContext dbContext,
        IOptions<PreventRegistrationOptions> preventRegistrationOptions,
        ICurrentClientProvider currentClientProvider) :
        base(userVerificationService, dbContext)
    {
        _journey = journey;
        PreventRegistrationOptions = preventRegistrationOptions.Value;
        _currentClientProvider = currentClientProvider;
    }

    public bool ShowBackLink =>
        CurrentClient?.ClientId is not { } clientId ||
        !PreventRegistrationOptions.ClientRedirects
            .Any(x => string.Equals(
                x.ClientId,
                clientId,
                StringComparison.OrdinalIgnoreCase));

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

    [BindProperty]
    [Display(Name = "Your email address", Description = "We’ll use this to send you a code to confirm your email address. Do not use a work or university email that you might lose access to.")]
    [Required(ErrorMessage = "Enter your email address")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string? Email { get; set; }

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

        var emailPinGenerationResult = await GenerateEmailPinForNewEmail(Email!, allowInstitutionEmails: true);

        if (!emailPinGenerationResult.Success)
        {
            return emailPinGenerationResult.Result!;
        }

        _journey.AuthenticationState.OnEmailSet(Email!, await IsInstitutionEmail(Email!));

        return await _journey.Advance(CurrentStep);
    }

    private void SetDefaultInputValues()
    {
        Email ??= _journey.AuthenticationState.EmailAddress;
    }

    public override async Task OnPageHandlerExecutionAsync(
        PageHandlerExecutingContext context,
        PageHandlerExecutionDelegate next)
    {
        CurrentClient = await _currentClientProvider.GetCurrentClient();
        await next();
    }
}
