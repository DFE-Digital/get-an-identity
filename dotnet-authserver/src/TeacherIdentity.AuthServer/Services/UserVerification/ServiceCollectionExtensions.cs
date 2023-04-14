namespace TeacherIdentity.AuthServer.Services.UserVerification;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserVerification(
        this IServiceCollection services,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        if (configuration.GetValue<bool>("UserVerification:UseFixedPin"))
        {
            services.AddSingleton<IUserVerificationService, FixedPinUserVerificationService>();

            services.AddOptions<FixedPinUserVerificationOptions>()
                .Bind(configuration.GetSection("UserVerification"))
                .ValidateDataAnnotations()
                .ValidateOnStart();
        }
        else
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

            if (environment.IsProduction())
            {
                services.AddSingleton<IRateLimitStore, RateLimitStore>();
            }
            else
            {
                services.AddSingleton<IRateLimitStore, NoopRateLimitStore>();
            }
        }

        services.AddSingleton<PinValidator>();

        return services;
    }
}
