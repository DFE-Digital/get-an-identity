using System.Diagnostics;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Playwright;
using Moq;
using OpenIddict.Server.AspNetCore;
using TeacherIdentity.AuthServer.EndToEndTests.Infrastructure;
using TeacherIdentity.AuthServer.EventProcessing;
using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.Services.DqtEvidence;

namespace TeacherIdentity.AuthServer.EndToEndTests;

/// <summary>
/// A fixture that hosts the authorization server and a test client and provides an <see cref="IBrowserContext"/>
/// for testing browser-based interactions.
/// </summary>
public class HostFixture : IAsyncLifetime
{
    public const string AuthServerBaseUrl = "http://localhost:55341";
    public const string ClientBaseUrl = "http://localhost:55342";
    public const string RedirectClientBaseUrl = "http://localhost:55343";

    private Host<TeacherIdentity.AuthServer.Program>? _authServerHost;
    private Host<TestClient.Program>? _clientHost;
    private Host<TestClient.Program>? _redirectclientHost;
    private IPlaywright? _playright;
    private bool _disposed = false;

    private readonly List<string> _capturedAccessTokens = new();

    public static string UserVerificationPin { get; } = "12345";

    public IServiceProvider AuthServerServices { get; private set; } = null!;

    public IConfiguration Configuration => AuthServerServices.GetRequiredService<IConfiguration>();

    public IBrowser Browser { get; private set; } = null!;

    public IReadOnlyCollection<string> CapturedAccessTokens => _capturedAccessTokens.AsReadOnly();

    public DbHelper? DbHelper { get; private set; }

    public Mock<IDqtApiClient> DqtApiClient { get; } = new();

    public Mock<IDqtEvidenceStorageService> DqtEvidenceStorageService { get; } = new();

    public CaptureEventObserver EventObserver => (CaptureEventObserver)AuthServerServices.GetRequiredService<IEventObserver>();

    public virtual string TestClientId { get; } = "testclient";
    public virtual string RedirectTestClientId { get; } = "redirecttestclient";

    public TestData TestData => AuthServerServices.GetRequiredService<TestData>();

    public ClientConfiguration[] Clients { get; private set; } = Array.Empty<ClientConfiguration>();

    public Task<IBrowserContext> CreateBrowserContext() =>
        Browser!.NewContextAsync(new()
        {
            BaseURL = ClientBaseUrl,
            ViewportSize = ViewportSize.NoViewport
        });

    public Task<IBrowserContext> CreateRedirectBrowserContext() =>
        Browser!.NewContextAsync(new()
        {
            BaseURL = RedirectClientBaseUrl,
            ViewportSize = ViewportSize.NoViewport
        });

    public async Task DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (Browser != null)
        {
            await Browser.DisposeAsync();
        }

        _playright?.Dispose();

        if (_clientHost != null)
        {
            await _clientHost.DisposeAsync();
        }

        if (_redirectclientHost != null)
        {
            await _redirectclientHost.DisposeAsync();
        }

        if (_authServerHost != null)
        {
            await _authServerHost.DisposeAsync();
        }
    }

    public async Task InitializeAsync()
    {
        var testConfiguration = GetTestConfiguration();

        DbHelper = new DbHelper(testConfiguration["AuthorizationServer:ConnectionStrings:DefaultConnection"] ?? throw new Exception("Connection string DefaultConnection is missing."));
        await DbHelper.ResetSchema();

        _authServerHost = CreateAuthServerHost(testConfiguration.GetSection("AuthorizationServer"));
        AuthServerServices = _authServerHost.Services;

        var clientHelper = new ClientConfigurationHelper(AuthServerServices);
        var clients = testConfiguration.GetSection("Clients").Get<ClientConfiguration[]>() ?? Array.Empty<ClientConfiguration>();

        await clientHelper.UpsertClients(clients);
        Clients = clients;

        _clientHost = CreateClientHost(testConfiguration.GetSection("TestClient"));
        _redirectclientHost = CreateRedirectClientHost(testConfiguration.GetSection("RedirectTestClient"));

        _playright = await Playwright.CreateAsync();

        var browserOptions = new BrowserTypeLaunchOptions()
        {
            Timeout = 10000,
            Args = new[] { "--start-maximized" }
        };

        if (Debugger.IsAttached)
        {
            browserOptions.Headless = false;
            browserOptions.Devtools = true;
            browserOptions.SlowMo = 250;
        }

        Browser = await _playright.Chromium.LaunchAsync(browserOptions);
    }

    public void OnTestStarting()
    {
        DqtApiClient.Reset();
        DqtEvidenceStorageService.Reset();
        EventObserver.Clear();
    }

    public void AssertEventIsUserSignedIn(
        Events.EventBase @event,
        Guid userId,
        bool expectOAuthProperties = true)
    {
        var userSignedIn = Assert.IsType<Events.UserSignedInEvent>(@event);
        Assert.Equal(DateTime.UtcNow, userSignedIn.CreatedUtc, TimeSpan.FromSeconds(10));
        Assert.Equal(userId, userSignedIn.User.UserId);

        if (expectOAuthProperties)
        {
            Assert.Equal(TestClientId, userSignedIn.ClientId);
            Assert.NotNull(userSignedIn.Scope);
        }
    }

    private Host<TeacherIdentity.AuthServer.Program> CreateAuthServerHost(IConfiguration configuration) =>
        Host<TeacherIdentity.AuthServer.Program>.CreateHost(
            AuthServerBaseUrl,
            builder =>
            {
                builder.UseConfiguration(configuration);

                builder.ConfigureServices(services =>
                {
                    services.Configure<OpenIddictServerAspNetCoreOptions>(options => options.DisableTransportSecurityRequirement = true);
                    services.AddSingleton<IDqtApiClient>(DqtApiClient.Object);
                    services.AddSingleton<IDqtEvidenceStorageService>(DqtEvidenceStorageService.Object);
                    services.AddSingleton(new BlobServiceClient("DefaultEndpointsProtocol=https;AccountName=MyAccount;AccountKey=MyAccountKey;EndpointSuffix=core.windows.net"));
                    services.AddSingleton<IEventObserver, CaptureEventObserver>();
                    services.AddSingleton<TestData>();

                    services.Configure<PreventRegistrationOptions>(opts =>
                    {
                        opts.ClientRedirects = new List<PreventRegistrationOptionsClientRedirect>()
                        {
                            new()
                            {
                                ClientId = RedirectTestClientId,
                                RedirectUri = "http://google.com"
                            }
                        };
                    });

                    // Publish events synchronously
                    services.AddSingleton<PublishEventsDbCommandInterceptor>();
                    services.Decorate<DbContextOptions<TeacherIdentityServerDbContext>>((inner, sp) =>
                    {
                        var coreOptionsExtension = inner.GetExtension<CoreOptionsExtension>();

                        return (DbContextOptions<TeacherIdentityServerDbContext>)inner.WithExtension(
                            coreOptionsExtension.WithInterceptors(new[] { sp.GetRequiredService<PublishEventsDbCommandInterceptor>() }));
                    });
                });
            });

    private Host<TestClient.Program> CreateClientHost(IConfiguration configuration) =>
        Host<TestClient.Program>.CreateHost(
            ClientBaseUrl,
            builder =>
            {
                builder.UseConfiguration(configuration);

                builder.ConfigureServices(services =>
                {
                    services.PostConfigure<OpenIdConnectOptions>("oidc", options =>
                    {
                        options.Events.OnTokenResponseReceived = ctx =>
                        {
                            _capturedAccessTokens.Add(ctx.TokenEndpointResponse.AccessToken);
                            return Task.CompletedTask;
                        };
                    });
                });
            });

    private Host<TestClient.Program> CreateRedirectClientHost(IConfiguration configuration) =>
        Host<TestClient.Program>.CreateHost(
            RedirectClientBaseUrl,
            builder =>
            {
                builder.UseConfiguration(configuration);

                builder.ConfigureServices(services =>
                {
                    services.PostConfigure<OpenIdConnectOptions>("oidc", options =>
                    {
                        options.Events.OnTokenResponseReceived = ctx =>
                        {
                            _capturedAccessTokens.Add(ctx.TokenEndpointResponse.AccessToken);
                            return Task.CompletedTask;
                        };
                    });
                });
            });

    private static IConfiguration GetTestConfiguration() =>
        new ConfigurationBuilder()
            .AddUserSecrets<HostFixture>()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
            .AddInMemoryCollection(new Dictionary<string, string?>()
            {
                { "AuthorizationServer:UserVerification:Pin", UserVerificationPin }
            })
            .Build();

    public sealed class Host<T> : IAsyncDisposable
        where T : class
    {
        private readonly KestrelWebApplicationFactory<T> _applicationFactory;

        private Host(KestrelWebApplicationFactory<T> applicationFactory)
        {
            _applicationFactory = applicationFactory;
        }

        public IServiceProvider Services => _applicationFactory.Services;

        public static Host<T> CreateHost(
            string url,
            Action<IWebHostBuilder> configureWebHostBuilder)
        {
            var applicationFactory = new KestrelWebApplicationFactory<T>(url, configureWebHostBuilder);
            _ = applicationFactory.Services;  // Starts the server
            return new Host<T>(applicationFactory);
        }

        public ValueTask DisposeAsync() => _applicationFactory.DisposeAsync();

        // See https://github.com/dotnet/aspnetcore/issues/4892
        private class KestrelWebApplicationFactory<TFactory> : WebApplicationFactory<TFactory>
            where TFactory : class
        {
            private readonly Action<IWebHostBuilder> _configureWebHostBuilder;
            private IHost? _host;

            public KestrelWebApplicationFactory(string url, Action<IWebHostBuilder> configureWebHostBuilder)
            {
                Url = url;
                _configureWebHostBuilder = configureWebHostBuilder;
            }

            public override IServiceProvider Services
            {
                get
                {
                    EnsureServer();
                    return _host!.Services!;
                }
            }

            public string Url { get; }

            protected override void ConfigureWebHost(IWebHostBuilder builder)
            {
                builder
                    .UseUrls(Url)
                    .UseEnvironment("EndToEndTests");

                _configureWebHostBuilder(builder);
            }

            protected override IHost CreateHost(IHostBuilder builder)
            {
                // We need to return a host configured with TestServer, even though we're not going to use it.
                // Configure an empty dummy web app with TestServer.
                var dummyBuilder = new HostBuilder();
                dummyBuilder.ConfigureWebHost(webBuilder => webBuilder.Configure(app => { }).UseTestServer());
                var testHost = dummyBuilder.Build();
                testHost.Start();

                builder.ConfigureWebHost(webHostBuilder => webHostBuilder.UseKestrel());

                _host = builder.Build();
                _host.Start();

                return testHost;
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);

                if (disposing)
                {
                    _host?.Dispose();
                }
            }

            private void EnsureServer()
            {
                if (_host is null)
                {
                    // This forces WebApplicationFactory to bootstrap the server
                    using var _ = CreateDefaultClient();
                }
            }
        }
    }
}
