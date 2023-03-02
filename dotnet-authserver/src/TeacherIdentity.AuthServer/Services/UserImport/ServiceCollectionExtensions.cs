using Azure.Storage.Blobs;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Services.UserSearch;

namespace TeacherIdentity.AuthServer.Services.UserImport;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddUserImport(
        this IServiceCollection services,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        if (!environment.IsUnitTests() && !environment.IsEndToEndTests())
        {
            services.AddOptions<UserImportOptions>()
                .Bind(configuration.GetSection("UserImport"))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddAzureClients(builder =>
            {
                builder.AddClient<BlobServiceClient, BlobClientOptions>((options, sp) =>
                {
                    var userImportOptions = sp.GetRequiredService<IOptions<UserImportOptions>>();
                    return new BlobServiceClient(userImportOptions.Value.StorageConnectionString, options);
                });
            });

            services.AddSingleton<IUserImportStorageService, BlobStorageUserImportStorageService>();
        }

        services.AddSingleton<INameSynonymsService, NameSynonymsService>();
        services.AddScoped<IUserSearchService, UserSearchService>();
        services.AddScoped<IUserImportProcessor, UserImportProcessor>();

        return services;
    }
}
