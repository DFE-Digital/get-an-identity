using Microsoft.AspNetCore.Mvc;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Journeys;

public abstract class SignInJourney
{
    protected SignInJourney(HttpContext httpContext, IdentityLinkGenerator linkGenerator)
    {
        AuthenticationState = httpContext.GetAuthenticationState();
        HttpContext = httpContext;
        LinkGenerator = linkGenerator;
    }

    public AuthenticationState AuthenticationState { get; }

    public HttpContext HttpContext { get; }

    public IdentityLinkGenerator LinkGenerator { get; }

    public abstract bool IsFinished();

    public abstract string GetStartStep();

    public abstract string GetStepUrl(string step);

    public abstract string? GetNextStep(string currentStep);

    public abstract string? GetPreviousStep(string currentStep);

    public abstract bool CanAccessStep(string step);

    public virtual string GetLastAccessibleStep()
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

    public string GetNextStepUrl(string currentStep)
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
        var previousStep = GetPreviousStep(currentStep) ??
            throw new InvalidOperationException($"Journey has no previous step (current step: '{currentStep}').");

        if (!CanAccessStep(previousStep))
        {
            throw new InvalidOperationException($"Previous step is not accessible (step: '{previousStep}').");
        }

        return GetStepUrl(previousStep);
    }

    public Task<IActionResult> OnEmailVerified(User? user)
    {
        if (!CanAccessStep(Steps.EmailConfirmation))
        {
            throw new InvalidOperationException("Email cannot be verified at this time.");
        }

        return OnEmailVerifiedCore(user);
    }

    protected abstract Task<IActionResult> OnEmailVerifiedCore(User? user);

    public static class Steps
    {
        public const string Email = $"{nameof(SignInJourney)}.{nameof(Email)}";
        public const string EmailConfirmation = $"{nameof(SignInJourney)}.{nameof(EmailConfirmation)}";
    }
}