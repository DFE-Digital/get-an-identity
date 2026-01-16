using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[CheckCanAccessStep(CurrentStep)]
public class EmailConfirmationModel : BaseEmailConfirmationPageModel
{
    private const string CurrentStep = SignInJourney.Steps.EmailConfirmation;

    private readonly SignInJourney _journey;
    private readonly TeacherIdentityServerDbContext _dbContext;


    public EmailConfirmationModel(
        SignInJourney journey,
        TeacherIdentityServerDbContext dbContext,
        IUserVerificationService userVerificationService,
        PinValidator pinValidator,
        IOptions<PreventRegistrationOptions> preventRegistrationOptions,
        ICurrentClientProvider currentClientProvider)
        : base(userVerificationService, pinValidator)
    {
        _journey = journey;
        _dbContext = dbContext;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

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

        var user = await _dbContext.Users.Where(u => u.EmailAddress == Email).SingleOrDefaultAsync();

        return await _journey.OnEmailVerified(user, CurrentStep);
    }
}
