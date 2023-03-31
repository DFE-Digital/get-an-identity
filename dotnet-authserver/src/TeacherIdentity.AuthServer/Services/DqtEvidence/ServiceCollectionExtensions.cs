using System.Net.Http.Headers;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.Services.UserImport;

namespace TeacherIdentity.AuthServer.Services.DqtEvidence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDqtEvidence(
        this IServiceCollection services,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        if (!environment.IsUnitTests() && !environment.IsEndToEndTests())
        {
            services.AddOptions<DqtEvidenceOptions>()
                .Bind(configuration.GetSection("DqtEvidence"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddAzureClients(builder =>
            {
                builder.AddClient<BlobServiceClient, BlobClientOptions>((options, sp) =>
                {
                    var dqtEvidenceOptions = sp.GetRequiredService<IOptions<DqtEvidenceOptions>>();
                    return new BlobServiceClient(dqtEvidenceOptions.Value.StorageConnectionString, options);
                });
            });

            services.AddSingleton<IDqtEvidenceStorageService, DqtEvidenceStorageService>();
        }

        return services;
    }
}
