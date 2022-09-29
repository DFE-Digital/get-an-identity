using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using TeacherIdentity.AuthServer.EventProcessing;
using TeacherIdentity.AuthServer.Models;
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

    public Mock<IDqtApiClient> DqtApiClient { get; } = new Mock<IDqtApiClient>();

    public Mock<IEmailSender> EmailSender { get; } = new Mock<IEmailSender>();

    public Spy<IEmailVerificationService> EmailVerificationService => Spy.Get(Services.GetRequiredService<IEmailVerificationService>());

    public CaptureEventObserver EventObserver => (CaptureEventObserver)Services.GetRequiredService<IEventObserver>();

    public async Task Initialize()
    {
        await DbHelper.EnsureSchema();

        await ConfigureTestClients();

        await ConfigureTestUsers();
    }

    public async Task ConfigureTestClients()
    {
        using var scope = Services.CreateAsyncScope();
        var manager = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();

        foreach (var client in TestClients.All)
        {
            await manager.CreateAsync(client);
        }
    }

    public async Task ConfigureTestUsers()
    {
        using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TeacherIdentityServerDbContext>();
        await TestUsers.CreateUsers(dbContext);
    }

    public void InitEventObserver() => EventObserver.Init();

    public void ResetMocks()
    {
        DqtApiClient.Reset();
        EmailSender.Reset();
        EmailVerificationService.Reset();
    }

    public async Task SignInUser(
        Guid journeyId,
        HttpClient httpClient,
        Guid userId,
        bool firstTimeSignInForEmail)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/_sign-in?{AuthenticationStateMiddleware.IdQueryParameterName}={Uri.EscapeDataString(journeyId.ToString())}")
        {
            Content = new FormUrlEncodedContentBuilder()
                .Add("UserId", userId.ToString())
                .Add("FirstTimeSignInForEmail", firstTimeSignInForEmail.ToString())
                .ToContent()
        };

        var response = await httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
    }

    public Task SignInUser(
        AuthenticationStateHelper authenticationStateHelper,
        HttpClient httpClient,
        Guid userId,
        bool firstTimeSignInForEmail)
    {
        return SignInUser(authenticationStateHelper.AuthenticationState.JourneyId, httpClient, userId, firstTimeSignInForEmail);
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
            services.AddSingleton<IEventObserver, CaptureEventObserver>();

            services.AddSingleton(DqtApiClient.Object);
            services.AddSingleton(EmailSender.Object);
            services.Decorate<IEmailVerificationService>(inner => Spy.Of(inner));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Ensure we can flow AsyncLocals from tests to the server
        builder.ConfigureServices(services => services.Configure<TestServerOptions>(o => o.PreserveExecutionContext = true));

        return base.CreateHost(builder);
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
