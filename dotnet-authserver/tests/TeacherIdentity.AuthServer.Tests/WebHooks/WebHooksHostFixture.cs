using TeacherIdentity.AuthServer.Models;
using TeacherIdentity.AuthServer.Notifications;
using TeacherIdentity.AuthServer.Notifications.WebHooks;
using TeacherIdentity.AuthServer.Tests.Infrastructure;

namespace TeacherIdentity.AuthServer.Tests.WebHooks;

public class WebHooksHostFixture : HostFixture
{
    private const string TestWebHookEndpointUri = "/test-webhooks";

    public readonly Guid TestWebHookId = Guid.NewGuid();

    public WebHooksHostFixture(TestConfiguration testConfiguration, DbHelper dbHelper)
        : base(testConfiguration, dbHelper)
    {
    }

    public IWebHookRequestObserver WebHookRequestObserver => Services.GetRequiredService<IWebHookRequestObserver>();

    public override async Task Initialize()
    {
        await DbHelper.EnsureSchema();
    }

    public void InitializeWebHookRequestObserver() => WebHookRequestObserver.Initialize();

    public async Task ConfigureTestWebHook(WebHookMessageTypes webHookMessageTypes, string secret)
    {
        using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TeacherIdentityServerDbContext>();
        dbContext.WebHooks.Add(new WebHook()
        {
            WebHookId = TestWebHookId,
            Enabled = true,
            Endpoint = $"http://localhost{TestWebHookEndpointUri}",
            Secret = secret,
            WebHookMessageTypes = webHookMessageTypes,
            Created = Clock.UtcNow,
            Updated = Clock.UtcNow
        });

        await dbContext.SaveChangesAsync();
    }

    protected override void ConfigureWebHooks(IServiceCollection services)
    {
        services
            .AddSingleton<INotificationPublisher, WebHookNotificationPublisher>()
            .AddSingleton<IWebHookNotificationSender>((sp) =>
            {
                var httpClient = CreateClient();
                return new WebHookNotificationSender(httpClient);
            })
            .AddSingleton<IWebHookRequestObserver, WebHookRequestObserver>();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.Configure(app =>
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapPost(TestWebHookEndpointUri, async (HttpContext context, IWebHookRequestObserver webHookRequestObserver) =>
                {
                    string? contentType = context.Request.Headers.ContentType;
                    string? signature = null;
                    if (context.Request.Headers.TryGetValue("X-Hub-Signature-256", out var signatureValue))
                    {
                        signature = signatureValue;
                    }

                    using var sr = new StreamReader(context.Request.Body);
                    var body = await sr.ReadToEndAsync();

                    var webHookRequest = new WebHookRequest()
                    {
                        ContentType = contentType,
                        Signature = signature,
                        Body = body
                    };

                    await webHookRequestObserver.OnWebHookRequestReceived(webHookRequest);
                });
            });
        });
    }
}
