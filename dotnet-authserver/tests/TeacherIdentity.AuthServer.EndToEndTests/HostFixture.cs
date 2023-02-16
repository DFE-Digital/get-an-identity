using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Playwright;
using Moq;
using OpenIddict.Server.AspNetCore;
using TeacherIdentity.AuthServer.EndToEndTests.Infrastructure;
using TeacherIdentity.AuthServer.EventProcessing;
using TeacherIdentity.AuthServer.Oidc;
using TeacherIdentity.AuthServer.Services.DqtApi;
using TeacherIdentity.AuthServer.Services.UserVerification;

namespace TeacherIdentity.AuthServer.EndToEndTests;

/// <summary>
/// A fixture that hosts the authorization server and a test client and provides an <see cref="IBrowserContext"/>
/// for testing browser-based interactions.
/// </summary>
public class HostFixture : IAsyncLifetime
{
    public const string AuthServerBaseUrl = "http://localhost:55341";
    public const string ClientBaseUrl = "http://localhost:55342";

    private Host<TeacherIdentity.AuthServer.Program>? _authServerHost;
    private Host<TestClient.Program>? _clientHost;
    private IPlaywright? _playright;
    private bool _disposed = false;

    private readonly List<string> _capturedAccessTokens = new();
    private readonly List<(string Email, string Pin)> _capturedEmailConfirmationPins = new();

    public IServiceProvider AuthServerServices { get; private set; } = null!;

    public IBrowser Browser { get; private set; } = null!;

    public IReadOnlyCollection<string> CapturedAccessTokens => _capturedAccessTokens.AsReadOnly();

    public IReadOnlyCollection<(string Email, string Pin)> CapturedEmailConfirmationPins => _capturedEmailConfirmationPins.AsReadOnly();

    public DbHelper? DbHelper { get; private set; }

    public Mock<IDqtApiClient> DqtApiClient { get; } = new();

    public IUserVerificationService? UserVerificationService { get; private set; }

    public CaptureEventObserver EventObserver => (CaptureEventObserver)AuthServerServices.GetRequiredService<IEventObserver>();

    public string TestClientId => GetTestConfiguration()["Client:ClientId"]!;

    public TestData TestData => AuthServerServices.GetRequiredService<TestData>();

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

        DbHelper = new DbHelper(testConfiguration["AuthorizationServer:ConnectionStrings:DefaultConnection"] ??
            throw new Exception("Connection string DefaultConnection is missing."));
        await DbHelper.ResetSchema();

        _authServerHost = CreateAuthServerHost(testConfiguration);
        AuthServerServices = _authServerHost.Services;

        var clientHelper = new ClientConfigurationHelper(AuthServerServices);
        var clients = testConfiguration.GetSection("Clients").Get<ClientConfiguration[]>() ?? Array.Empty<ClientConfiguration>();

        await clientHelper.UpsertClients(clients);

        _clientHost = CreateClientHost(testConfiguration);

        _playright = await Playwright.CreateAsync();

        var browserOptions = new BrowserTypeLaunchOptions()
        {
            Timeout = 10000
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
        EventObserver.Clear();
    }

    private Host<TeacherIdentity.AuthServer.Program> CreateAuthServerHost(IConfiguration testConfiguration) =>
        Host<TeacherIdentity.AuthServer.Program>.CreateHost(
            AuthServerBaseUrl,
            builder =>
            {
                builder.UseConfiguration(testConfiguration.GetSection("AuthorizationServer"));

                builder.ConfigureServices(services =>
                {
                    services.Configure<OpenIddictServerAspNetCoreOptions>(options => options.DisableTransportSecurityRequirement = true);
                    services.AddSingleton<IDqtApiClient>(DqtApiClient.Object);
                    services.Decorate<IUserVerificationService>(inner =>
                        new CapturePinsUserVerificationServiceDecorator(inner, (email, pin) => _capturedEmailConfirmationPins.Add((email, pin))));
                    services.AddSingleton<IEventObserver, CaptureEventObserver>();
                    services.AddSingleton<TestData>();
                });
            });

    private Host<TestClient.Program> CreateClientHost(IConfiguration testConfiguration) =>
        Host<TestClient.Program>.CreateHost(
            ClientBaseUrl,
            builder =>
            {
                builder.UseConfiguration(testConfiguration.GetSection("Client"));

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

    private class CapturePinsUserVerificationServiceDecorator : IUserVerificationService
    {
        public delegate void CapturePin(string email, string pin);

        private readonly IUserVerificationService _inner;
        private readonly CapturePin _capturePin;

        public CapturePinsUserVerificationServiceDecorator(IUserVerificationService inner, CapturePin capturePin)
        {
            _inner = inner;
            _capturePin = capturePin;
        }

        public async Task<PinGenerationResult> GenerateEmailPin(string email)
        {
            var pinGenerationResult = await _inner.GenerateEmailPin(email);
            _capturePin(email, pinGenerationResult.Pin!);
            return pinGenerationResult;
        }

        public Task<PinVerificationFailedReasons> VerifyEmailPin(string email, string pin) => _inner.VerifyEmailPin(email, pin);
        public Task<PinGenerationResult> GenerateSmsPin(string mobileNumber)
        {
            throw new NotImplementedException();
        }

        public Task<PinVerificationFailedReasons> VerifySmsPin(string mobileNumber, string pin)
        {
            throw new NotImplementedException();
        }
    }
}
