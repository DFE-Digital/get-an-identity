using System.Net.Http.Headers;
using Microsoft.Extensions.Options;

namespace TeacherIdentity.AuthServer.Services.DqtApi;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDqtApi(
        this IServiceCollection services,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        if (environment.IsProduction())
        {
            services.AddOptions<DqtApiOptions>()
                .Bind(configuration.GetSection("DqtApi"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services
                .AddSingleton<IDqtApiClient, DqtApiClient>()
                .AddHttpClient<IDqtApiClient, DqtApiClient>((sp, httpClient) =>
                {
                    var options = sp.GetRequiredService<IOptions<DqtApiOptions>>();
                    httpClient.BaseAddress = new Uri(options.Value.BaseAddress);
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.Value.ApiKey);
                });
        }
        else
        {
            services.AddTransient<IDqtApiClient, FakeDqtApiClient>();
        }

        return services;
    }
}
