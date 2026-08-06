using StackExchange.Redis;
using XanhNow.Security.Application.Abstractions.RateLimiting;
using XanhNow.Security.Infrastructure.Integration.Options;

namespace XanhNow.Security.Infrastructure.Integration.Redis;

internal sealed class RedisRateLimitService : IRateLimitService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly RedisRuntimeState _state;
    private readonly RedisIntegrationOptions _options;

    public RedisRateLimitService(RedisConnectionProvider redis, RedisRuntimeState state, SecurityIntegrationOptions options)
    {
        _redis = redis.Connection;
        _state = state;
        _options = options.Redis;
    }

    public async ValueTask<RateLimitDecision> CheckAsync(string key, int maxRequests, TimeSpan window, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (maxRequests <= 0 || window <= TimeSpan.Zero)
        {
            return RateLimitDecision.Deny(window > TimeSpan.Zero ? window : TimeSpan.FromSeconds(1));
        }

        var namespacedKey = $"{_options.KeyPrefix}:rate:{key}";
        var now = DateTimeOffset.UtcNow;

        if (_redis is not null)
        {
            var db = _redis.GetDatabase();
            var count = await db.StringIncrementAsync(namespacedKey);
            if (count == 1)
            {
                await db.KeyExpireAsync(namespacedKey, window);
            }

            if (count <= maxRequests)
            {
                return RateLimitDecision.Allow();
            }

            var ttl = await db.KeyTimeToLiveAsync(namespacedKey);
            return RateLimitDecision.Deny(ttl is { } retryAfter && retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.FromSeconds(1));
        }

        var record = _state.Counters.AddOrUpdate(
            namespacedKey,
            _ => new RedisCounterRecord(1, now.Add(window)),
            (_, current) => current.WindowExpiresAt <= now ? new RedisCounterRecord(1, now.Add(window)) : current with { Count = current.Count + 1 });

        if (record.Count <= maxRequests)
        {
            return RateLimitDecision.Allow();
        }

        var localRetryAfter = record.WindowExpiresAt - now;
        return RateLimitDecision.Deny(localRetryAfter > TimeSpan.Zero ? localRetryAfter : TimeSpan.FromSeconds(1));
    }
}
