using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Testing;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.Services.Email;
using TeacherIdentity.AuthServer.Services.EmailVerification;
using TeacherIdentity.AuthServer.State;
using TeacherIdentity.AuthServer.TestCommon;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests;

public class HostFixture : WebApplicationFactory<TeacherIdentity.AuthServer.Program>
{
    private readonly TestConfiguration _testConfiguration;

    public HostFixture(TestConfiguration testConfiguration, DbHelper dbHelper)
    {
        _testConfiguration = testConfiguration;
        DbHelper = dbHelper;
    }

    public IConfiguration Configuration => Services.GetRequiredService<IConfiguration>();

    public DbHelper DbHelper { get; }

    public IDqtApiClient? DqtApiClient { get; private set; }

    public IEmailSender? EmailSender { get; private set; }

    public IEmailVerificationService? EmailVerificationService { get; private set; }

    public async Task Initialize()
    {
        await DbHelper.EnsureSchema();

        await ConfigureTestClients();
    }

    public void ResetMocks()
    {
        ClearRecordedCalls(EmailVerificationService);
        ClearRecordedCalls(EmailSender);

        static void ClearRecordedCalls(object? fakedObject)
        {
            if (fakedObject is not null)
            {
                Fake.ClearRecordedCalls(fakedObject);
            }
        }
    }

    public async Task SignInUser(
        AuthenticationStateHelper authenticationStateHelper,
        HttpClient httpClient,
        Guid userId,
        bool firstTimeUser,
        string? trn)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/_sign-in?{authenticationStateHelper.ToQueryParam()}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("UserId", userId.ToString())
                .Add("FirstTimeUser", firstTimeUser.ToString())
                .Add("Trn", trn ?? string.Empty)
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
        builder.UseConfiguration(_testConfiguration.Configuration);

        builder.ConfigureServices(services =>
        {
            // Remove the built-in antiforgery filters
            // (we want to be able to POST directly from a test without having to set antiforgery cookies etc.)
            services.AddSingleton<IPageApplicationModelProvider, RemoveAutoValidateAntiforgeryPageApplicationModelProvider>();

            // Add the /_sign-in endpoint
            services.AddSingleton<IStartupFilter>(new AddSignInEndpointStartupFilter());

            // Disable the HTTPS requirement for OpenIddict
            services.Configure<OpenIddictServerAspNetCoreOptions>(options => options.DisableTransportSecurityRequirement = true);

            services.AddSingleton<IAuthenticationStateProvider, TestAuthenticationStateProvider>();
            services.AddSingleton<TestData>();
            services.AddSingleton<IClock, TestClock>();

            AddSpy<IEmailVerificationService>(spy => EmailVerificationService = spy);
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
