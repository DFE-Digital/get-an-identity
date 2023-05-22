using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Journeys;

public class SignInJourneyProvider
{
    public SignInJourney GetSignInJourney(AuthenticationState authenticationState, HttpContext httpContext)
    {
        if (authenticationState.TryGetOAuthState(out var oAuthState) && authenticationState.UserRequirements.RequiresTrnLookup())
        {
            if (oAuthState.TrnRequirementType == TrnRequirementType.Legacy || oAuthState.HasScope(CustomScopes.Trn))
            {
                return ActivatorUtilities.CreateInstance<LegacyTrnJourney>(httpContext.RequestServices, httpContext);
            }

            return authenticationState.HasTrnToken ?
                ActivatorUtilities.CreateInstance<TrnTokenSignInJourney>(httpContext.RequestServices, httpContext) :
                ActivatorUtilities.CreateInstance<CoreSignInJourneyWithTrnLookup>(httpContext.RequestServices, httpContext);
        }

        if (authenticationState.UserRequirements.HasFlag(UserRequirements.StaffUserType))
        {
            return ActivatorUtilities.CreateInstance<StaffSignInJourney>(httpContext.RequestServices, httpContext);
        }

        return ActivatorUtilities.CreateInstance<CoreSignInJourney>(httpContext.RequestServices, httpContext);
    }
}
