using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using TeacherIdentity.AuthServer.EventProcessing;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests;

public class DbFixture : IAsyncLifetime
{
    public DbFixture(TestConfiguration testConfiguration, DbHelper dbHelper)
    {
        var configuration = testConfiguration.Configuration;
        ConnectionString = configuration.GetConnectionString("DefaultConnection") ??
            throw new Exception("Connection string DefaultConnection is missing.");
        DbHelper = dbHelper;
        Services = GetServices();
    }

    public IClock Clock => Services.GetRequiredService<IClock>();

    public string ConnectionString { get; }

    public DbHelper DbHelper { get; }

    public IServiceProvider Services { get; }

    public TestData TestData => Services.GetRequiredService<TestData>();

    public TeacherIdentityServerDbContext GetDbContext() => Services.GetRequiredService<TeacherIdentityServerDbContext>();

    public IDbContextFactory<TeacherIdentityServerDbContext> GetDbContextFactory() => Services.GetRequiredService<IDbContextFactory<TeacherIdentityServerDbContext>>();

    public async Task InitializeAsync()
    {
        await DbHelper.EnsureSchema();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    private IServiceProvider GetServices()
    {
        var services = new ServiceCollection();

        services.AddDbContext<TeacherIdentityServerDbContext>(
            options => TeacherIdentityServerDbContext.ConfigureOptions(options, ConnectionString),
            contextLifetime: ServiceLifetime.Transient);

        services.AddDbContextFactory<TeacherIdentityServerDbContext>(
             options => TeacherIdentityServerDbContext.ConfigureOptions(options, ConnectionString));

        var preventChangesToEntitiesInterceptor = new PreventChangesToEntitiesInterceptor<User, Guid>(TestUsers.All.Select(u => u.UserId), (user) => user.UserId);
        services.AddSingleton(preventChangesToEntitiesInterceptor);
        services.Decorate<DbContextOptions<TeacherIdentityServerDbContext>>((inner, sp) =>
        {
            var coreOptionsExtension = inner.GetExtension<CoreOptionsExtension>();
            return (DbContextOptions<TeacherIdentityServerDbContext>)inner.WithExtension(
                coreOptionsExtension.WithInterceptors(new[]
                {
                    sp.GetRequiredService<PreventChangesToEntitiesInterceptor<User, Guid>>(),
                }));
        });

        services.AddSingleton<TestData>();
        services.AddSingleton<IClock, TestClock>();
        services.AddSingleton<IEventObserver, NoopEventObserver>();

        return services.BuildServiceProvider();
    }
}
