using TeacherIdentity.AuthServer.Services.BackgroundJobs;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.Services.Email;
using TeacherIdentity.AuthServer.Services.EmailVerification;
using TeacherIdentity.AuthServer.Services.TrnLookup;
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
            .AddDqtApi(environment, configuration)
            .AddEmail(environment, configuration)
            .AddEmailVerification(environment, configuration)
            .AddTrnLookup(configuration)
            .AddSingleton<Redactor>()
            .AddZendesk(environment, configuration);
    }
}
