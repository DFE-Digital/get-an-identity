using Notify.Client;

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
            services.AddSingleton(new NotificationClient(configuration["NotifyApiKey"]));
            services.AddSingleton<IEmailSender, NotifyEmailSender>();

            // Use Hangfire for scheduling emails in the background (so we get retries etc.).
            // As the implementation needs to be able to resolve itself we need two service registrations here;
            // one for the interface (that decorates the 'base' notify implementation) and another for the concrete type.
            services.Decorate<IEmailSender, BackgroundEmailSender>();
            services.AddSingleton<BackgroundEmailSender>(sp => (BackgroundEmailSender)sp.GetRequiredService<IEmailSender>());
        }
        else
        {
            services.AddSingleton<IEmailSender, NoopEmailSender>();
        }

        return services;
    }
}
