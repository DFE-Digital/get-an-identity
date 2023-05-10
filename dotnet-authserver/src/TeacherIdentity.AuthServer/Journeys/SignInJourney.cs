using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Journeys;

public abstract class SignInJourney
{
    protected SignInJourney(
        HttpContext httpContext,
        IdentityLinkGenerator linkGenerator,
        CreateUserHelper createUserHelper)
    {
        AuthenticationState = httpContext.GetAuthenticationState();
        HttpContext = httpContext;
        LinkGenerator = linkGenerator;
        CreateUserHelper = createUserHelper;
    }

    public virtual async Task<IActionResult> CreateUser(string currentStep)
    {
        var user = await CreateUserHelper.CreateUser(AuthenticationState);

        AuthenticationState.OnUserRegistered(user);
        await AuthenticationState.SignIn(HttpContext);

        return new RedirectResult(GetNextStepUrl(currentStep));
    }

    public virtual Task<RedirectResult> Advance(string currentStep)
    {
        return Task.FromResult(new RedirectResult(GetNextStepUrl(currentStep)));
    }

    public AuthenticationState AuthenticationState { get; }

    public HttpContext HttpContext { get; }

    public IdentityLinkGenerator LinkGenerator { get; }

    protected CreateUserHelper CreateUserHelper { get; }

    protected abstract bool IsFinished();

    protected abstract string GetStartStep();

    protected abstract string GetStepUrl(string step);

    protected abstract string? GetNextStep(string currentStep);

    protected abstract string? GetPreviousStep(string currentStep);

    public abstract bool CanAccessStep(string step);

    public virtual string GetLastAccessibleStepUrl(string? requestedStep)
    {
        // This is used when the user tries to access a page in the journey that's not accessible.
        // We want to redirect them somewhere valid for the journey but we don't have a current step
        // to invoke GetNextStep() with.
        //
        // Find the final step in the journey that's accessible and return that.

        var step = GetStartStep();

        while (true)
        {
            var nextStep = GetNextStep(step);

            if (nextStep is null)
            {
                break;
            }

            step = nextStep;
        }

        // step is now the final step in journey. Walk backwards until we find a step that's accessible.

        while (!CanAccessStep(step))
        {
            step = GetPreviousStep(step);

            if (step is null)
            {
                throw new InvalidOperationException("Journey has no available steps.");
            }
        }

        return GetStepUrl(step);
    }

    public string GetStartStepUrl() => GetStepUrl(GetStartStep());

    public virtual async Task<IActionResult> OnEmailVerified(User? user, string currentStep)
    {
        if (user is not null && user.UserType == UserType.Staff)
        {
            return new ViewResult()
            {
                ViewName = "StaffUserForbidden",
                StatusCode = StatusCodes.Status403Forbidden
            };
        }

        AuthenticationState.OnEmailVerified(user);

        if (user is not null)
        {
            await AuthenticationState.SignIn(HttpContext);
        }

        return await Advance(currentStep);
    }

    public virtual string GetNextStepUrl(string currentStep)
    {
        if (IsFinished())
        {
            return AuthenticationState.PostSignInUrl;
        }

        var nextStep = GetNextStep(currentStep) ??
            throw new InvalidOperationException($"Journey has no next step (current step: '{currentStep}').");

        if (!CanAccessStep(nextStep))
        {
            throw new InvalidOperationException($"Next step is not accessible (step: '{nextStep}').");
        }

        return GetStepUrl(nextStep);
    }

    public string GetPreviousStepUrl(string currentStep)
    {
        if (!TryGetPreviousStepUrl(currentStep, out var stepUrl))
        {
            throw new InvalidOperationException($"Journey has no previous step (current step: '{currentStep}').");
        }

        return stepUrl;
    }

    public bool TryGetPreviousStepUrl(string currentStep, [NotNullWhen(true)] out string? stepUrl)
    {
        var previousStep = GetPreviousStep(currentStep);

        if (previousStep is null || !CanAccessStep(previousStep))
        {
            stepUrl = default;
            return false;
        }

        stepUrl = GetStepUrl(previousStep);
        return true;
    }

    public virtual bool IsCompleted() => IsFinished();

    public static class Steps
    {
        public const string Email = $"{nameof(SignInJourney)}.{nameof(Email)}";
        public const string EmailConfirmation = $"{nameof(SignInJourney)}.{nameof(EmailConfirmation)}";
        public const string TrnInUse = $"{nameof(SignInJourney)}.{nameof(TrnInUse)}";
        public const string TrnInUseChooseEmail = $"{nameof(SignInJourney)}.{nameof(TrnInUseChooseEmail)}";
        public const string TrnInUseResendTrnOwnerEmailConfirmation = $"{nameof(SignInJourney)}.{nameof(TrnInUseResendTrnOwnerEmailConfirmation)}";
    }
}
