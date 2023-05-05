using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TeacherIdentity.AuthServer.Journeys;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Pages.Common;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.Pages.SignIn;

[BindProperties]
public class ResendEmailConfirmationModel : BaseEmailPageModel
{
    private readonly SignInJourney _journey;

    public ResendEmailConfirmationModel(
        IUserVerificationService userVerificationService,
        SignInJourney journey,
        TeacherIdentityServerDbContext dbContext) :
        base(userVerificationService, dbContext)
    {
        _journey = journey;
    }

    [Display(Name = "Email address")]
    [Required(ErrorMessage = "Enter your email address")]
    [EmailAddress(ErrorMessage = "Enter a valid email address")]
    public string? Email { get; set; }

    public void OnGet()
    {
        Email = HttpContext.GetAuthenticationState().EmailAddress;
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

        return Redirect(_journey.LinkGenerator.EmailConfirmation());
    }

    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        var authenticationState = context.HttpContext.GetAuthenticationState();

        if (authenticationState.EmailAddress is null)
        {
            context.Result = Redirect(_journey.LinkGenerator.Email());
        }
        else if (authenticationState.EmailAddressVerified)
        {
            context.Result = Redirect(_journey.GetLastAccessibleStepUrl(requestedStep: null));
        }
    }
}
