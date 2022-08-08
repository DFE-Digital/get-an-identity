using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Testing;
using TeacherIdentity.AuthServer.TestCommon;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace TeacherIdentity.AuthServer.Tests;

public class HostFixture : WebApplicationFactory<TeacherIdentity.AuthServer.Program>, IAsyncLifetime
{
    public IConfiguration Configuration => Services.GetRequiredService<IConfiguration>();

    public DbHelper? DbHelper { get; private set; }

    public async Task InitializeAsync()
    {
        DbHelper = new DbHelper(Configuration.GetConnectionString("DefaultConnection"));
        await DbHelper.ResetSchema();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("UnitTests");

        // N.B. Don't use builder.ConfigureAppConfiguration here since it runs *after* the entry point
        // i.e. Program.cs and that has a dependency on IConfiguration
        builder.UseConfiguration(GetTestConfiguration());

        builder.ConfigureServices(services =>
        {
            // Remove the built-in antiforgery filters
            // (we want to be able to POST directly from a test without having to set antiforgery cookies etc.)
            services.AddSingleton<IPageApplicationModelProvider, RemoveAutoValidateAntiforgeryPageApplicationModelProvider>();

        });
    }

    private static IConfiguration GetTestConfiguration() =>
        new ConfigurationBuilder()
            .AddUserSecrets<HostFixture>()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

    private class RemoveAutoValidateAntiforgeryPageApplicationModelProvider : IPageApplicationModelProvider
    {
        public int Order => int.MaxValue;

        public void OnProvidersExecuted(PageApplicationModelProviderContext context)
        {
        }

        public void OnProvidersExecuting(PageApplicationModelProviderContext context)
        {
            var pageApplicationModel = context.PageApplicationModel;

            var autoValidateAttribute = pageApplicationModel.Filters.OfType<AutoValidateAntiforgeryTokenAttribute>().SingleOrDefault();
            if (autoValidateAttribute is not null)
            {
                pageApplicationModel.Filters.Remove(autoValidateAttribute);
            }
        }
    }
}
