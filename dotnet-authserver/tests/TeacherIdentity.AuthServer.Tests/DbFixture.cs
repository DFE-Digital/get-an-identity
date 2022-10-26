using TeacherIdentity.AuthServer.EventProcessing;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.TestCommon;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests;

public class DbFixture : IAsyncLifetime
{
    public DbFixture(TestConfiguration testConfiguration, DbHelper dbHelper)
    {
        var configuration = testConfiguration.Configuration;
        ConnectionString = configuration.GetConnectionString("DefaultConnection");
        DbHelper = dbHelper;
        Services = GetServices();
    }

    public string ConnectionString { get; }

    public DbHelper DbHelper { get; }

    public IServiceProvider Services { get; }

    public TestData TestData => Services.GetRequiredService<TestData>();

    public TeacherIdentityServerDbContext GetDbContext() => Services.GetRequiredService<TeacherIdentityServerDbContext>();
    public IRequestClientIpProvider RequestClientIpProvider => Services.GetRequiredService<IRequestClientIpProvider>();

    public async Task InitializeAsync()
    {
        await DbHelper.EnsureSchema();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    private IServiceProvider GetServices()
    {
        var services = new ServiceCollection();

        services.AddDbContext<TeacherIdentityServerDbContext>(
            options =>
            {
                TeacherIdentityServerDbContext.ConfigureOptions(options, ConnectionString);
            },
            contextLifetime: ServiceLifetime.Transient);

        services.AddSingleton<TestData>();
        services.AddSingleton<IClock, TestClock>();
        services.AddSingleton<IEventObserver, NoopEventObserver>();
        services.AddSingleton<IRequestClientIpProvider, TestRequestClientIpProvider>();

        return services.BuildServiceProvider();
    }
}