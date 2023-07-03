using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests.WebHooks;

public class TestBase : IAsyncLifetime
{
    protected TestBase(WebHooksHostFixture hostFixture)
    {
        HostFixture = hostFixture;
        HostFixture.InitializeWebHookRequestObserver();
    }

    public async Task InitializeAsync()
    {
        using var scope = HostFixture.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TeacherIdentityServerDbContext>();
        await dbContext.Database.ExecuteSqlAsync($"delete from webhooks");

        var memoryCache = HostFixture.Services.GetRequiredService<IMemoryCache>();
        memoryCache.Remove(MemoryCacheKeys.WebHooks);
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public IWebHookRequestObserver WebHookRequestObserver => HostFixture.WebHookRequestObserver;

    public WebHooksHostFixture HostFixture { get; }

    public Task ConfigureTestWebHook(WebHookMessageTypes webHookMessageTypes, string secret) => HostFixture.ConfigureTestWebHook(webHookMessageTypes, secret);
}
