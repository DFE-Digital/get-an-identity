namespace TeacherIdentity.AuthServer.Services.UserVerification;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserVerification(
        this IServiceCollection services,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        services.AddTransient<IUserVerificationService, UserVerificationService>();

        services.AddOptions<UserVerificationOptions>()
            .Bind(configuration.GetSection("UserVerification"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RateLimitStoreOptions>()
            .Bind(configuration.GetSection("UserVerificationRateLimit"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<PinValidator>();

        if (environment.IsProduction())
        {
            services.AddSingleton<IRateLimitStore, RateLimitStore>();
        }
        else
        {
            services.AddSingleton<IRateLimitStore, NoopRateLimitStore>();
        }

        return services;
    }
}
