using System.Collections.Concurrent;
using TeacherIdentity.AuthServer.Models;

namespace TeacherIdentity.AuthServer.Tests;

public partial class TestData
{
    private readonly IServiceProvider _serviceProvider;
    private readonly TestClock _clock;
    private readonly Random _random = new();
    private readonly ConcurrentBag<string> _trns = new();

    public TestData(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _clock = (TestClock)serviceProvider.GetRequiredService<IClock>();
    }

    public string GenerateTrn()
    {
        lock (_random)
        {
            string trn;

            do
            {
                trn = _random.Next(minValue: 1000000, maxValue: 1999999).ToString();
            }
            while (_trns.Contains(trn));

            return trn;
        }
    }

    public async Task<T> WithDbContext<T>(Func<TeacherIdentityServerDbContext, Task<T>> action)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<TeacherIdentityServerDbContext>();
        return await action(dbContext);
    }

    public async Task WithDbContext(Func<TeacherIdentityServerDbContext, Task> action) =>
        await WithDbContext(async dbContext =>
        {
            await action(dbContext);
            return 0;
        });
}
