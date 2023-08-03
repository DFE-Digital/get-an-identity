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

            return provider.GetSignInJourney(authenticationState, httpContext);
        });

        services
            .AddTransient<TrnLookupHelper>()
            .AddTransient<UserHelper>()
            .AddTransient<TrnTokenHelper>();

        return services;
    }
}
