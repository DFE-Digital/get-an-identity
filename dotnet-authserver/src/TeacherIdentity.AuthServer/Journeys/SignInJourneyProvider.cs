using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Journeys;

public class SignInJourneyProvider
{
    public SignInJourney GetSignInJourney(AuthenticationState authenticationState, HttpContext httpContext)
    {
        var signInJourneyType = typeof(CoreSignInJourney);

        if (authenticationState.TryGetOAuthState(out var oAuthState) && authenticationState.UserRequirements.RequiresTrnLookup())
        {
#pragma warning disable CS0612 // Type or member is obsolete
            if (oAuthState.TrnRequirementType == TrnRequirementType.Legacy)
            {
                throw new NotSupportedException("The Legacy sign in journey is no longer supported.");
            }
#pragma warning restore CS0612 // Type or member is obsolete

            signInJourneyType = authenticationState.HasTrnToken ? typeof(TrnTokenSignInJourney) :
                authenticationState.RequiresTrnVerificationLevelElevation == true ? typeof(ElevateTrnVerificationLevelJourney) :
                typeof(CoreSignInJourneyWithTrnLookup);
        }

        if (authenticationState.UserRequirements.HasFlag(UserRequirements.StaffUserType))
        {
            signInJourneyType = typeof(StaffSignInJourney);
        }

        return (SignInJourney)ActivatorUtilities.CreateInstance(httpContext.RequestServices, signInJourneyType, httpContext);
    }
}
