using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.TestCommon;

namespace TeacherIdentity.AuthServer.Tests;

public class DbFixture : IAsyncLifetime
{
    public DbFixture()
    {
        var configuration = GetConfiguration();
        ConnectionString = configuration.GetConnectionString("DefaultConnection");
        DbHelper = new DbHelper(ConnectionString);
        Services = GetServices();
    }

    public string ConnectionString { get; }

    public DbHelper DbHelper { get; }

    public IServiceProvider Services { get; }

    public TestData TestData => Services.GetRequiredService<TestData>();

    public TeacherIdentityServerDbContext GetDbContext() => Services.GetRequiredService<TeacherIdentityServerDbContext>();

    public async Task InitializeAsync()
    {
        await DbHelper.ResetSchema();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    private IConfiguration GetConfiguration() =>
        new ConfigurationBuilder()
            .AddUserSecrets<DbFixture>()
            .AddEnvironmentVariables()
            .Build();

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

        return services.BuildServiceProvider();
    }
}
