using TeacherIdentity.AuthServer.Services.AzureBlobService;
using TeacherIdentity.AuthServer.Services.BackgroundJobs;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.Services.DqtEvidence;
using TeacherIdentity.AuthServer.Services.Establishment;
using TeacherIdentity.AuthServer.Services.EventPublishing;
using TeacherIdentity.AuthServer.Services.Notification;
using TeacherIdentity.AuthServer.Services.TrnTokens;
using TeacherIdentity.AuthServer.Services.UserImport;
using TeacherIdentity.AuthServer.Services.UserVerification;
using TeacherIdentity.AuthServer.Services.Zendesk;

namespace TeacherIdentity.AuthServer.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuthServerServices(
        this IServiceCollection services,
        IWebHostEnvironment environment,
        IConfiguration configuration,
        string postgresConnectionString)
    {
        return services
            .AddBackgroundJobs(environment, postgresConnectionString)
            .AddGias(environment, configuration)
            .AddAzureBlobServiceClient(environment, configuration)
            .AddUserImport(environment, configuration)
            .AddDqtEvidence(environment, configuration)
            .AddDqtApi(environment, configuration)
            .AddEmail(environment, configuration)
            .AddUserVerification(environment, configuration)
            .AddSingleton<Redactor>()
            .AddZendesk(environment, configuration)
            .AddEventPublishing(environment)
            .AddTrnTokens(configuration);
    }
}
