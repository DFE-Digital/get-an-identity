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
public class ExistingAccountEmailConfirmation : BaseEmailConfirmationPageModel
{
    private const string CurrentStep = CoreSignInJourney.Steps.ExistingAccountEmailConfirmation;

    private readonly SignInJourney _journey;
    private readonly TeacherIdentityServerDbContext _dbContext;

    public ExistingAccountEmailConfirmation(
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

    public override string? Email => HttpContext.GetAuthenticationState().ExistingAccountEmail;
    public new User? User { get; set; }

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

        authenticationState.OnExistingAccountVerified(User!);
        await authenticationState.SignIn(HttpContext);

        return await _journey.Advance(CurrentStep);
    }

    public override async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var authenticationState = HttpContext.GetAuthenticationState();

        User = await _dbContext.Users.Where(u => u.UserType == UserType.Default && u.EmailAddress == Email).SingleOrDefaultAsync();

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
