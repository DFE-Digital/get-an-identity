using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn.Register;

[CheckCanAccessStep(CurrentStep)]
public class EmailConfirmationModel : BaseEmailConfirmationPageModel
{
    private const string CurrentStep = CoreSignInJourney.Steps.EmailConfirmation;

    private readonly SignInJourney _journey;
    private readonly TeacherIdentityServerDbContext _dbContext;

    public EmailConfirmationModel(
        IUserVerificationService userVerificationService,
        PinValidator pinValidator,
        TeacherIdentityServerDbContext dbContext, SignInJourney journey)
        : base(userVerificationService, pinValidator)
    {
        _dbContext = dbContext;
        _journey = journey;
    }

    public string BackLink => _journey.GetPreviousStepUrl(CurrentStep);

    [BindProperty]
    [Display(Name = "Confirmation code")]
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

        var pinVerificationFailedReasons = await UserVerificationService.VerifyEmailPin(Email!, Code!);

        if (pinVerificationFailedReasons != PinVerificationFailedReasons.None)
        {
            return await HandlePinVerificationFailed(pinVerificationFailedReasons);
        }

        var authenticationState = HttpContext.GetAuthenticationState();
        var permittedUserTypes = authenticationState.GetPermittedUserTypes();

        var user = await _dbContext.Users.Where(u => u.EmailAddress == Email).SingleOrDefaultAsync();

        if (user is not null && !permittedUserTypes.Contains(user.UserType))
        {
            return new ForbidResult();
        }

        HttpContext.GetAuthenticationState().OnEmailVerified(user);

        if (user is not null)
        {
            await authenticationState.SignIn(HttpContext);
        }

        return await _journey.Advance(CurrentStep);
    }
}
