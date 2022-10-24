namespace TeacherIdentity.AuthServer.Services.TrnLookup;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTrnLookup(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<FindALostTrnIntegrationOptions>()
            .Bind(configuration.GetSection("FindALostTrnIntegration"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddTransient<FindALostTrnIntegrationHelper>();

        return services;
    }
}
