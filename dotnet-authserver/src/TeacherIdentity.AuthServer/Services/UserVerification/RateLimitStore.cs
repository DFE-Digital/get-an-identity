using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace TeacherIdentity.AuthServer.Services.UserVerification;

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
        await _connectionMultiplexer.GetDatabase().ScriptEvaluateAsync(_atomicIncrement, new { key = new RedisKey($"pin-failed-{clientIp}"), timeout = _rateLimitOptions.Value.PinVerificationFailureTimeoutSeconds });
    }

    public async Task<bool> IsClientIpBlockedForPinVerification(string clientIp)
    {
        var failureCountForClientIp = await _connectionMultiplexer.GetDatabase().StringGetAsync(new RedisKey($"pin-failed-{clientIp}"));
        return !(failureCountForClientIp.IsNullOrEmpty || (int)failureCountForClientIp <= _rateLimitOptions.Value.PinVerificationMaxFailures);
    }

    public async Task<bool> IsClientIpBlockedForPinGeneration(string clientIp)
    {
        var failureCountForClientIp = await _connectionMultiplexer.GetDatabase().StringGetAsync(new RedisKey($"pin-generation-{clientIp}"));
        return !(failureCountForClientIp.IsNullOrEmpty || (int)failureCountForClientIp <= _rateLimitOptions.Value.PinGenerationMaxFailures);
    }
    public async Task AddPinGeneration(string clientIp)
    {
        await _connectionMultiplexer.GetDatabase().ScriptEvaluateAsync(_atomicIncrement, new { key = new RedisKey($"pin-generation-{clientIp}"), timeout = _rateLimitOptions.Value.PinGenerationTimeoutSeconds });
    }
}
