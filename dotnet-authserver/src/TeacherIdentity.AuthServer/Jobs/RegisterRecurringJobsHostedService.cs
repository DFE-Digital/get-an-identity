using Hangfire;
using Microsoft.Extensions.Options;
using TeacherIdentity.AuthServer.Services.Establishment;

namespace TeacherIdentity.AuthServer.Jobs;

public class RegisterRecurringJobsHostedService : IHostedService
{
    private readonly IOptions<GiasOptions> _giasOptions;
    private readonly IRecurringJobManager _recurringJobManager;

    public RegisterRecurringJobsHostedService(
        IRecurringJobManager recurringJobManager,
        IOptions<GiasOptions> giasOptions)
    {
        _recurringJobManager = recurringJobManager;
        _giasOptions = giasOptions;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        RegisterJobs();
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private void RegisterJobs()
    {
        _recurringJobManager.AddOrUpdate<PruneTokensJob>(nameof(PruneTokensJob), job => job.Execute(CancellationToken.None), Cron.Hourly);
        _recurringJobManager.AddOrUpdate<RefreshEstablishmentDomainsJob>(nameof(RefreshEstablishmentDomainsJob), job => job.Execute(CancellationToken.None), _giasOptions.Value.RefreshEstablishmentDomainsJobSchedule);
        _recurringJobManager.AddOrUpdate<PurgeConfirmationPinsJob>(nameof(PurgeConfirmationPinsJob), job => job.Execute(CancellationToken.None), Cron.Daily);
    }
}
