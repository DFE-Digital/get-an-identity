using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Journeys;

public class StaffSignInJourney : SignInJourney
{
    public StaffSignInJourney(HttpContext httpContext, IdentityLinkGenerator linkGenerator, CreateUserHelper createUserHelper)
        : base(httpContext, linkGenerator, createUserHelper)
    {
    }

    public override bool CanAccessStep(string step) => step switch
    {
        Steps.Email => !AuthenticationState.EmailAddressVerified,
        Steps.EmailConfirmation => !AuthenticationState.EmailAddressVerified && AuthenticationState.EmailAddressSet,
        _ => false
    };

    protected override string? GetNextStep(string currentStep) => currentStep switch
    {
        Steps.Email => Steps.EmailConfirmation,
        _ => null
    };

    protected override string? GetPreviousStep(string currentStep) => currentStep switch
    {
        Steps.EmailConfirmation => Steps.Email,
        _ => null
    };

    protected override string GetStartStep() => Steps.Email;

    protected override string GetStepUrl(string step) => step switch
    {
        Steps.Email => LinkGenerator.Email(),
        Steps.EmailConfirmation => LinkGenerator.EmailConfirmation(),
        _ => throw new ArgumentException($"Unknown step: '{step}'.")
    };

    protected override bool IsFinished() => AuthenticationState.UserId.HasValue;

    public async override Task<IActionResult> OnEmailVerified(User? user, string currentStep)
    {
        if (user is null || user.UserType != UserType.Staff)
        {
            return new ForbidResult();
        }

        AuthenticationState.OnEmailVerified(user);
        await AuthenticationState.SignIn(HttpContext);

        return await Advance(currentStep);
    }
}
