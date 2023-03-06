using Microsoft.Extensions.Options;

namespace TeacherIdentity.AuthServer.Services.Establishment;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddGias(
        this IServiceCollection services,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        if (!environment.IsUnitTests() && !environment.IsEndToEndTests())
        {
            services.AddOptions<GiasOptions>()
                .Bind(configuration.GetSection("Gias"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services
                .AddSingleton<IEstablishmentMasterDataService, CsvDownloadEstablishmentMasterDataService>()
                .AddHttpClient<IEstablishmentMasterDataService, CsvDownloadEstablishmentMasterDataService>((sp, httpClient) =>
                {
                    var options = sp.GetRequiredService<IOptions<GiasOptions>>();
                    httpClient.BaseAddress = new Uri(options.Value.BaseDownloadAddress);
                });
        }

        return services;
    }
}
