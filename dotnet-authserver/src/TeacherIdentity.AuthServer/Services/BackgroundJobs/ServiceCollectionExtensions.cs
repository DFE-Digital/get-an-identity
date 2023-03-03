using Hangfire;
using Hangfire.PostgreSql;
using TeacherIdentity.AuthServer.Jobs;

namespace TeacherIdentity.AuthServer.Services.BackgroundJobs;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddBackgroundJobs(
        this IServiceCollection services,
        IWebHostEnvironment environment,
        string postgresConnectionString)
    {
        if (!environment.IsUnitTests() && !environment.IsEndToEndTests())
        {
            services.AddHangfire(configuration => configuration
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(postgresConnectionString));

            services.AddHangfireServer();

            services.AddSingleton<IHostedService, RegisterRecurringJobsHostedService>();
        }

        if (environment.IsProduction())
        {
            services.AddSingleton<IBackgroundJobScheduler, HangfireBackgroundJobScheduler>();
        }
        else
        {
            services.AddSingleton<IBackgroundJobScheduler, ExecuteImmediatelyJobScheduler>();
        }

        return services;
    }
}
