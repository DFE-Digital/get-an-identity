namespace TeacherIdentity.AuthServer.Services.EventPublishing;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddEventPublishing(
        this IServiceCollection services,
        IWebHostEnvironment environment)
    {
        if (!environment.IsUnitTests())
        {
            services.AddSingleton<IHostedService, PublishEventsBackgroundService>();
        }

        return services;
    }
}
