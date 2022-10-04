using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace TeacherIdentity.AuthServer.Services.EmailVerification;

public class RateLimitStore : IRateLimitStore
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IOptions<RateLimitStoreOptions> _rateLimitOptions;
    public RateLimitStore(IConnectionMultiplexer connectionMultiplexer,
        IOptions<RateLimitStoreOptions> rateLimitOptions)
    {
        _rateLimitOptions = rateLimitOptions;
        _connectionMultiplexer = connectionMultiplexer;
    }

    private static readonly LuaScript _atomicIncrement = LuaScript.Prepare("redis.call(\"INCR\", @key) local ttl = redis.call(\"TTL\", @key) if ttl == -1 then redis.call(\"EXPIRE\", @key, @timeout) end");

    public async Task AddFailedPinVerification(string clientIp)
    {
        await _connectionMultiplexer.GetDatabase().ScriptEvaluateAsync(_atomicIncrement, new { key = new RedisKey($"pin-failed-{clientIp}"), timeout = _rateLimitOptions.Value.FailureTimeoutSeconds });
    }

    public async Task<bool> IsClientIpBlocked(string clientIp)
    {
        var failureCountForClientIp = await _connectionMultiplexer.GetDatabase().StringGetAsync(new RedisKey($"pin-failed-{clientIp}"));
        return !(failureCountForClientIp.IsNullOrEmpty || (int)failureCountForClientIp <= _rateLimitOptions.Value.MaxFailures);
    }
}
