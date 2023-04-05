using TeacherIdentity.AuthServer.Services.DqtApi;

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

            services.AddSingleton<IDqtEvidenceStorageService, DqtEvidenceStorageService>();
        }

        return services;
    }
}
