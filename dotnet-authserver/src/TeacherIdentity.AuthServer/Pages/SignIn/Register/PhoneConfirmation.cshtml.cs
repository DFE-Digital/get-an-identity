using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckJourneyType(typeof(CoreSignInJourney), typeof(CoreSignInJourneyWithTrnLookup), typeof(TrnTokenSignInJourney))]
[CheckCanAccessStep(CurrentStep)]
public class PhoneConfirmation : BasePhoneConfirmationPageModel
{
    private const string CurrentStep = CoreSignInJourney.Steps.PhoneConfirmation;

    private readonly SignInJourney _journey;
    private readonly TeacherIdentityServerDbContext _dbContext;

    public PhoneConfirmation(
        IUserVerificationService userVerificationService,
        PinValidator pinValidator,
        SignInJourney journey,
        TeacherIdentityServerDbContext dbContext)
        : base(userVerificationService, pinValidator)
    {
        _journey = journey;
        _dbContext = dbContext;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

    [BindProperty]
    [Display(Name = "Security code")]
    public override string? Code { get; set; }

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

        var parsedMobileNumber = Models.MobileNumber.Parse(MobileNumber!);
        var pinVerificationFailedReasons = await UserVerificationService.VerifySmsPin(parsedMobileNumber, Code!);

        if (pinVerificationFailedReasons != PinVerificationFailedReasons.None)
        {
            return await HandlePinVerificationFailed(pinVerificationFailedReasons);
        }

        var user = await _dbContext.Users
            .Where(u => u.NormalizedMobileNumber! == parsedMobileNumber)
            .SingleOrDefaultAsync();

        return await _journey.OnMobileVerified(user, CurrentStep);
    }
}
