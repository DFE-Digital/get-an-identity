namespace TeacherIdentity.AuthServer.Services.Email;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEmail(
        this IServiceCollection services,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        if (environment.IsProduction())
        {
            services.AddOptions<NotifyOptions>()
                .Bind(configuration.GetSection("Notify"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddSingleton<IEmailSender, NotifyEmailSender>();
        }
        else
        {
            services.AddSingleton<IEmailSender, NoopEmailSender>();
        }

        return services;
    }
}
