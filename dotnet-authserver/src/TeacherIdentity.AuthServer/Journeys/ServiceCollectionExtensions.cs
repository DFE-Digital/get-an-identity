namespace TeacherIdentity.AuthServer.Journeys;

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

        services
            .AddTransient<TrnLookupHelper>()
            .AddTransient<CreateUserHelper>();

        services.AddTransient<LegacyTrnJourney>(
            sp => sp.GetRequiredService<SignInJourney>() as LegacyTrnJourney ??
                throw new InvalidOperationException($"Current journey is not a {nameof(LegacyTrnJourney)}."));

        return services;
    }
}