using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using TeacherIdentity.AuthServer.EventProcessing;
using TeacherIdentity.AuthServer.Infrastructure.Security;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Notifications;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.Services.DqtEvidence;
using TeacherIdentity.AuthServer.Services.Notification;
using TeacherIdentity.AuthServer.Services.UserImport;
using TeacherIdentity.AuthServer.Services.UserVerification;
using TeacherIdentity.AuthServer.Services.Zendesk;
using TeacherIdentity.AuthServer.State;
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

    public TestClock Clock => TestScopedServices.Current.Clock;

    public IConfiguration Configuration => Services.GetRequiredService<IConfiguration>();

    public DbHelper DbHelper { get; }

    public Mock<IDqtApiClient> DqtApiClient => TestScopedServices.Current.DqtApiClient;

    public CaptureEventObserver EventObserver => (CaptureEventObserver)Services.GetRequiredService<IEventObserver>();

    public Mock<INotificationSender> NotificationSender => TestScopedServices.Current.NotificationSender;

    public Mock<INotificationPublisher> NotificationPublisher => TestScopedServices.Current.NotificationPublisher;

    public Mock<IRateLimitStore> RateLimitStore => TestScopedServices.Current.RateLimitStore;

    public IRequestClientIpProvider RequestClientIpProvider => Services.GetRequiredService<IRequestClientIpProvider>();

    public SpyRegistry SpyRegistry => TestScopedServices.Current.SpyRegistry;

    public Mock<IUserImportStorageService> UserImportCsvStorageService => TestScopedServices.Current.UserImportCsvStorageService;

    public Mock<IDqtEvidenceStorageService> DqtEvidenceStorageService => TestScopedServices.Current.DqtEvidenceStorageService;

    public Spy<IUserVerificationService> UserVerificationService => SpyRegistry.Get<IUserVerificationService>();

    public Mock<IZendeskApiWrapper> ZendeskApiWrapper => TestScopedServices.Current.ZendeskApiWrapper;

    public virtual async Task Initialize()
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

    public void ResetMocks() => _ = TestScopedServices.Current;

    // N.B. Don't call this from InitializeAsync - it won't work
    public void SetUserId(Guid? userId) => Services.GetRequiredService<CurrentUserIdContainer>().CurrentUserId.Value = userId;

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

            // Add the filter that automatically signs in users if the active AuthenticationState has a UserId set
            services.AddMvc(options =>
            {
                options.Filters.Add(new SignInUserPageFilter());
            });

            // Add the custom test cookie authentication handler
            services.AddSingleton<CurrentUserIdContainer>();
            services.PostConfigure<AuthenticationOptions>(options =>
                options.Schemes.Single(s => s.Name == AuthenticationSchemes.Cookie).HandlerType = typeof(TestCookieAuthenticationHandler));

            services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.Backchannel = CreateClient();
            });

            // Disable tracking sign in events from the Delegated authentication handler
            services.PostConfigure<DelegatedAuthenticationOptions>(AuthenticationSchemes.Delegated, options =>
            {
                options.OnUserSignedIn = (httpContext, principal) => Task.CompletedTask;
            });

            // Disable the HTTPS requirement for OpenIddict
            services.Configure<OpenIddictServerAspNetCoreOptions>(options => options.DisableTransportSecurityRequirement = true);

            // Add Pages defined in this test project
            services.AddRazorPages().AddApplicationPart(typeof(HostFixture).Assembly);

            // Publish events synchronously + prevent tests from updating "fixed" default users
            services.AddSingleton<PublishEventsDbCommandInterceptor>();
            var preventChangesToEntitiesInterceptor = new PreventChangesToEntitiesInterceptor<User, Guid>(TestUsers.All.Select(u => u.UserId), (user) => user.UserId);
            services.AddSingleton(preventChangesToEntitiesInterceptor);
            services.Decorate<DbContextOptions<TeacherIdentityServerDbContext>>((inner, sp) =>
            {
                var coreOptionsExtension = inner.GetExtension<CoreOptionsExtension>();

                return (DbContextOptions<TeacherIdentityServerDbContext>)inner.WithExtension(
                    coreOptionsExtension.WithInterceptors(new IInterceptor[]
                    {
                        sp.GetRequiredService<PublishEventsDbCommandInterceptor>(),
                        sp.GetRequiredService<PreventChangesToEntitiesInterceptor<User, Guid>>(),
                    }));
            });

            services.AddSingleton<IAuthenticationStateProvider, TestAuthenticationStateProvider>();
            services.AddSingleton<TestData>();
            services.AddTransient<IClock>(_ => Clock);
            services.AddSingleton<IEventObserver, CaptureEventObserver>();
            services.AddTransient(_ => DqtApiClient.Object);
            services.AddTransient(_ => NotificationSender.Object);
            services.AddTransient(_ => RateLimitStore.Object);
            services.AddTransient<IRequestClientIpProvider, TestRequestClientIpProvider>();
            services.Decorate<IUserVerificationService>(inner => SpyRegistry.Get<IUserVerificationService>().Wrap(inner));
            services.AddTransient(_ => ZendeskApiWrapper.Object);
            services.AddTransient(_ => UserImportCsvStorageService.Object);
            services.AddTransient(_ => DqtEvidenceStorageService.Object);
            services.AddSingleton(new BlobServiceClient("DefaultEndpointsProtocol=https;AccountName=MyAccount;AccountKey=MyAccountKey;EndpointSuffix=core.windows.net"));
            ConfigureWebHooks(services);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Ensure we can flow AsyncLocals from tests to the server
        builder.ConfigureServices(services => services.Configure<TestServerOptions>(o => o.PreserveExecutionContext = true));

        return base.CreateHost(builder);
    }

    protected virtual void ConfigureWebHooks(IServiceCollection services)
    {
        services.AddTransient(_ => NotificationPublisher.Object);
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
