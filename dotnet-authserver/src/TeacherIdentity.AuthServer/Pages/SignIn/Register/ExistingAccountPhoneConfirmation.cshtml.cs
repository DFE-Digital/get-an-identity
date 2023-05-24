using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckJourneyType(typeof(CoreSignInJourney), typeof(CoreSignInJourneyWithTrnLookup), typeof(TrnTokenSignInJourney))]
[CheckCanAccessStep(CurrentStep)]
public class ExistingAccountPhoneConfirmation : BasePhoneConfirmationPageModel
{
    private const string CurrentStep = CoreSignInJourney.Steps.ExistingAccountPhoneConfirmation;

    private readonly SignInJourney _journey;
    private readonly TeacherIdentityServerDbContext _dbContext;

    public ExistingAccountPhoneConfirmation(
        IUserVerificationService userVerificationService,
        PinValidator pinValidator,
        TeacherIdentityServerDbContext dbContext,
        SignInJourney journey)
        : base(userVerificationService, pinValidator)
    {
        _dbContext = dbContext;
        _journey = journey;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

    public override string? MobileNumber => HttpContext.GetAuthenticationState().ExistingAccountMobileNumber;

    public new User? User { get; set; }

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

        var authenticationState = HttpContext.GetAuthenticationState();

        authenticationState.OnExistingAccountVerified(User!);
        await authenticationState.SignIn(HttpContext);

        return await _journey.Advance(CurrentStep);
    }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        var parsedMobileNumber = Models.MobileNumber.Parse(MobileNumber!);
        User = await _dbContext.Users.SingleOrDefaultAsync(u => u.UserType == UserType.Default && u.NormalizedMobileNumber! == parsedMobileNumber);

        if (User is null)
        {
            context.Result = NotFound();
            return;
        }

        if (!authenticationState.GetPermittedUserTypes().Contains(User.UserType))
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }
}
