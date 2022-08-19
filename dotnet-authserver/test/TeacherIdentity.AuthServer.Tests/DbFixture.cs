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
    }

    public string ConnectionString { get; }

    public DbHelper DbHelper { get; }

    public TeacherIdentityServerDbContext GetDbContext() => new(ConnectionString);

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
}
