using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Journeys;

public class SignInJourneyProvider
{
    public SignInJourney GetSignInJourney(AuthenticationState authenticationState, HttpContext httpContext)
    {
        if (authenticationState.TryGetOAuthState(out var oAuthState) && authenticationState.UserRequirements.RequiresTrnLookup())
        {
            var useLegacyTrnJourney = oAuthState.TrnRequirementType == TrnRequirementType.Legacy || oAuthState.HasScope(CustomScopes.Trn);

            return useLegacyTrnJourney ?
                ActivatorUtilities.CreateInstance<LegacyTrnJourney>(httpContext.RequestServices, httpContext) :
                ActivatorUtilities.CreateInstance<CoreSignInJourneyWithTrnLookup>(httpContext.RequestServices, httpContext);
        }

        if (authenticationState.UserRequirements.HasFlag(UserRequirements.StaffUserType))
        {
            return ActivatorUtilities.CreateInstance<StaffSignInJourney>(httpContext.RequestServices, httpContext);
        }

        return ActivatorUtilities.CreateInstance<CoreSignInJourney>(httpContext.RequestServices, httpContext);
    }
}
