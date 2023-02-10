using Microsoft.Extensions.Diagnostics.HealthChecks;
using ZendeskApi.Client;
using ZendeskApi.Client.Options;

namespace TeacherIdentity.AuthServer.Services.Zendesk;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddZendesk(
        this IServiceCollection services,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        if (!environment.IsUnitTests())
        {
            if (configuration.GetValue<bool>("Zendesk:UseFakeClient", defaultValue: true))
            {
                services.AddSingleton<IZendeskApiWrapper, FakeZendeskApiWrapper>();
            }
            else
            {
                services.AddOptions<ZendeskOptions>().Bind(configuration.GetSection("Zendesk"));

                services.AddScoped<IZendeskClient, ZendeskClient>();
                services.AddScoped<IZendeskApiClient, ZendeskApiClientFactory>();
                services.AddScoped<IZendeskApiWrapper, ZendeskApiWrapper>();
                services.AddTransient<ZendeskHealthCheck>();

                services.AddHealthChecks().Add(
                    new HealthCheckRegistration(
                        "Zendesk",
                        sp => sp.GetRequiredService<ZendeskHealthCheck>(),
                        HealthStatus.Unhealthy,
                        tags: null));
            }
        }

        return services;
    }
}
