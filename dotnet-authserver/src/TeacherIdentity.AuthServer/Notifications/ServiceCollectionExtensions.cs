using Azure.Messaging.ServiceBus;
using TeacherIdentity.AuthServer.Notifications.WebHooks;

namespace TeacherIdentity.AuthServer.Notifications;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNotifications(
        this IServiceCollection services,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        if (!environment.IsUnitTests())
        {
            if (environment.IsProduction() ||
                configuration.GetValue<bool?>("ServiceBusWebHookNotificationPublisher") == true)
            {
                var sbClient = new ServiceBusClient(configuration.GetConnectionString("ServiceBus"));

                services.AddSingleton(sbClient);
                services.AddSingleton<ServiceBusWebHookNotificationPublisher>();
                services.AddSingleton<INotificationPublisher>(sp => sp.GetRequiredService<ServiceBusWebHookNotificationPublisher>());
                services.AddHostedService(sp => sp.GetRequiredService<ServiceBusWebHookNotificationPublisher>());

                services.AddOptions<ServiceBusWebHookOptions>()
                    .Bind(configuration.GetSection("WebHooks"))
                    .ValidateDataAnnotations()
                    .ValidateOnStart();
            }
            else
            {
                services.AddSingleton<INotificationPublisher, WebHookNotificationPublisher>();
            }

            services.AddOptions<WebHookOptions>()
                .Bind(configuration.GetSection("WebHooks"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services
                .AddSingleton<IWebHookNotificationSender, WebHookNotificationSender>()
                .AddHttpClient<IWebHookNotificationSender, WebHookNotificationSender>(httpClient =>
                {
                    httpClient.Timeout = TimeSpan.FromSeconds(30);
                    httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Teacher Identity");
                })
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
                {
                    UseCookies = false,
                    AllowAutoRedirect = false,
                    PreAuthenticate = true
                });
        }

        return services;
    }
}
