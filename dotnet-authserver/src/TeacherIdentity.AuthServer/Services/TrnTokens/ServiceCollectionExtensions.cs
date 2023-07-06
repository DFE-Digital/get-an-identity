namespace TeacherIdentity.AuthServer.Services.TrnTokens;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddTrnTokens(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<TrnTokenOptions>()
            .Bind(configuration.GetSection("TrnTokens"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddTransient<TrnTokenService>();

        return services;
    }
}
