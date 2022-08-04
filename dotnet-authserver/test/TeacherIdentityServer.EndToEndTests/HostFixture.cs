using System.Diagnostics;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Playwright;
using OpenIddict.Server.AspNetCore;

namespace TeacherIdentityServer.EndToEndTests;

/// <summary>
/// A fixture that hosts the authorization server and a test client and provides an <see cref="IBrowserContext"/>
/// for testing browser-based interactions.
/// </summary>
public class HostFixture : IAsyncLifetime
{
    public const string AuthServerBaseUrl = "http://localhost:55341";
    public const string ClientBaseUrl = "http://localhost:55342";

    private Host<TeacherIdentityServer.Program>? _authServerHost;
    private Host<Client.Program>? _clientHost;
    private IPlaywright? _playright;
    private bool _disposed = false;

    public IServiceProvider AuthServerServices { get; private set; } = null!;

    public IBrowser Browser { get; private set; } = null!;

    public DbHelper? DbHelper { get; private set; }

    public Task<IBrowserContext> CreateBrowserContext() =>
        Browser!.NewContextAsync(new BrowserNewContextOptions() { BaseURL = ClientBaseUrl });

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

        if (_authServerHost != null)
        {
            await _authServerHost.DisposeAsync();
        }
    }

    public async Task InitializeAsync()
    {
        var testConfiguration = GetTestConfiguration();

        DbHelper = new DbHelper(testConfiguration["AuthorizationServer:ConnectionStrings:DefaultConnection"]);
        await DbHelper.ResetSchema();

        _authServerHost = CreateAuthServerHost(testConfiguration);
        AuthServerServices = _authServerHost.Services;

        _clientHost = CreateClientHost(testConfiguration);

        _playright = await Playwright.CreateAsync();

        var browserOptions = new BrowserTypeLaunchOptions();

        if (Debugger.IsAttached)
        {
            browserOptions.Headless = false;
            browserOptions.Devtools = true;
            browserOptions.SlowMo = 250;
        }

        Browser = await _playright.Chromium.LaunchAsync(browserOptions);
    }

    private static Host<TeacherIdentityServer.Program> CreateAuthServerHost(IConfiguration testConfiguration) =>
        Host<TeacherIdentityServer.Program>.CreateHost(
            AuthServerBaseUrl,
            builder =>
            {
                builder.UseConfiguration(testConfiguration.GetSection("AuthorizationServer"));

                builder.ConfigureServices(services =>
                {
                    services.Configure<OpenIddictServerAspNetCoreOptions>(options => options.DisableTransportSecurityRequirement = true);
                });
            });

    private static Host<Client.Program> CreateClientHost(IConfiguration testConfiguration) =>
        Host<Client.Program>.CreateHost(
            ClientBaseUrl,
            builder =>
            {
                builder.UseConfiguration(testConfiguration.GetSection("Client"));
            });

    private static IConfiguration GetTestConfiguration() =>
        new ConfigurationBuilder()
            .AddUserSecrets<HostFixture>()
            .AddJsonFile("appsettings.json")
            .AddEnvironmentVariables()
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
