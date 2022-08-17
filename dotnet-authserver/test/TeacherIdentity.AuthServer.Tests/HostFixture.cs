using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Testing;
using OpenIddict.Abstractions;
using TeacherIdentity.AuthServer.Services;
using TeacherIdentity.AuthServer.State;
using TeacherIdentity.AuthServer.TestCommon;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace TeacherIdentity.AuthServer.Tests;

public class HostFixture : WebApplicationFactory<TeacherIdentity.AuthServer.Program>, IAsyncLifetime
{
    public IConfiguration Configuration => Services.GetRequiredService<IConfiguration>();

    public DbHelper? DbHelper { get; private set; }

    public IEmailConfirmationService? EmailConfirmationService { get; private set; }
    public IDqtApiClient? DqtApiClient { get; private set; }
    public IEmailSender? EmailSender { get; private set; }

    public async Task InitializeAsync()
    {
        DbHelper = new DbHelper(Configuration.GetConnectionString("DefaultConnection"));
        await DbHelper.ResetSchema();

        await ConfigureTestClients();
    }

    Task IAsyncLifetime.DisposeAsync() => Task.CompletedTask;

    public void ResetMocks()
    {
        ClearRecordedCalls(EmailConfirmationService);
        ClearRecordedCalls(EmailSender);

        void ClearRecordedCalls(object? fakedObject)
        {
            if (fakedObject is not null)
            {
                Fake.ClearRecordedCalls(fakedObject);
            }
        }
    }

    public async Task SignInUser(Guid userId, AuthenticationStateHelper authenticationStateHelper, HttpClient httpClient)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/_sign-in?{authenticationStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("UserId", userId.ToString())
                .ToContent()
        };

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

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

            // Add the /_sign-in endpoint
            services.AddSingleton<IStartupFilter>(new AddSignInEndpointStartupFilter());

            services.AddSingleton<IAuthenticationStateProvider, TestAuthenticationStateProvider>();
            services.AddSingleton<TestData>();
            services.AddSingleton<IClock, TestClock>();

            AddSpy<IEmailConfirmationService>(spy => EmailConfirmationService = spy);
            AddSpy<IEmailSender>(spy => EmailSender = spy);
            DqtApiClient = A.Fake<IDqtApiClient>();
            services.AddSingleton<IDqtApiClient>(DqtApiClient);

            void AddSpy<T>(Action<T> assignSpy) where T : class
            {
                services.Decorate<T>(inner =>
                {
                    var spy = A.Fake<T>(o => o.Wrapping(inner));
                    assignSpy(spy);
                    return spy;
                });
            }
        });
    }

    private async Task ConfigureTestClients()
    {
        using var scope = Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        foreach (var client in TestClients.All)
        {
            await manager.CreateAsync(client);
        }
    }

    private static IConfiguration GetTestConfiguration() =>
        new ConfigurationBuilder()
            .AddUserSecrets<HostFixture>()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .Build();

    private class AddSignInEndpointStartupFilter : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
        {
            next(app);

            app.MapWhen(
                ctx => ctx.Request.Path == new PathString("/_sign-in") && ctx.Request.Method == HttpMethods.Post,
                app => app.UseMiddleware<SignInUserMiddleware>());
        };
    }

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
