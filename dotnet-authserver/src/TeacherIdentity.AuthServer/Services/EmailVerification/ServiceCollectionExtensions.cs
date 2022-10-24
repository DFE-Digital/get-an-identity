namespace TeacherIdentity.AuthServer.Services.EmailVerification;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEmailVerification(
        this IServiceCollection services,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        services.AddTransient<IEmailVerificationService, EmailVerificationService>();

        services.AddOptions<EmailVerificationOptions>()
            .Bind(configuration.GetSection("EmailVerification"))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RateLimitStoreOptions>()
            .Bind(configuration.GetSection("EmailVerificationRateLimit"))
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
