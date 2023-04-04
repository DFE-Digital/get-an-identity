using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;

namespace TeacherIdentity.AuthServer.Services.AzureBlobService;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAzureBlobServiceClient(
        this IServiceCollection services,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        if (!environment.IsUnitTests() && !environment.IsEndToEndTests())
        {
            services.AddAzureClients(builder =>
            {
                builder.AddClient((BlobClientOptions options, IServiceProvider sp) =>
                    new BlobServiceClient(configuration.GetConnectionString("DataProtectionBlobStorage"), options));
            });
        }

        return services;
    }
}
