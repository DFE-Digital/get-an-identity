using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;

namespace TeacherIdentity.AuthServer.Journeys;

public class SignInJourneyProvider
{
    public SignInJourney GetSignInJourneyState(AuthenticationState authenticationState, HttpContext httpContext)
    {
        if (authenticationState.TryGetOAuthState(out var oAuthState) && oAuthState.RequiresTrnLookup)
        {
            var useLegacyTrnJourney = oAuthState.TrnRequirementType == TrnRequirementType.Legacy || oAuthState.HasScope(CustomScopes.Trn);

            return useLegacyTrnJourney ?
                ActivatorUtilities.CreateInstance<LegacyTrnJourney>(httpContext.RequestServices, authenticationState, httpContext) :
                ActivatorUtilities.CreateInstance<CoreSignInJourneyWithTrnLookup>(httpContext.RequestServices, authenticationState, httpContext);
        }

        if (authenticationState.UserRequirements.HasFlag(UserRequirements.StaffUserType))
        {
            return ActivatorUtilities.CreateInstance<StaffSignInJourney>(httpContext.RequestServices, authenticationState, httpContext);
        }

        return ActivatorUtilities.CreateInstance<CoreSignInJourney>(httpContext.RequestServices, authenticationState, httpContext);
    }
}

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSignInJourneyStateProvider(this IServiceCollection services)
    {
        services.AddSingleton<SignInJourneyProvider>();

        services.AddTransient<SignInJourney>(sp =>
        {
            var provider = sp.GetRequiredService<SignInJourneyProvider>();
            var httpContextAccessor = sp.GetRequiredService<IHttpContextAccessor>();

            var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("No current HttpContext");
            var authenticationState = httpContext.GetAuthenticationState();

            return provider.GetSignInJourneyState(authenticationState, httpContext);
        });

        return services;
    }
}
