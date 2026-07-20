using XanhNow.Security.Application.Abstractions.RateLimiting;
using XanhNow.Security.Infrastructure.Integration.Options;

namespace XanhNow.Security.Infrastructure.Integration.Redis;

internal sealed class RedisRateLimitService : IRateLimitService
{
    private readonly RedisRuntimeState _state;
    private readonly RedisIntegrationOptions _options;

    public RedisRateLimitService(RedisRuntimeState state, SecurityIntegrationOptions options)
    {
        _state = state;
        _options = options.Redis;
    }

    public ValueTask<RateLimitDecision> CheckAsync(string key, int maxRequests, TimeSpan window, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (maxRequests <= 0 || window <= TimeSpan.Zero)
        {
            return ValueTask.FromResult(RateLimitDecision.Deny(window > TimeSpan.Zero ? window : TimeSpan.FromSeconds(1)));
        }

        var now = DateTimeOffset.UtcNow;
        var namespacedKey = $"{_options.KeyPrefix}:rate:{key}";
        var record = _state.Counters.AddOrUpdate(
            namespacedKey,
            _ => new RedisCounterRecord(1, now.Add(window)),
            (_, current) => current.WindowExpiresAt <= now ? new RedisCounterRecord(1, now.Add(window)) : current with { Count = current.Count + 1 });

        if (record.Count <= maxRequests)
        {
            return ValueTask.FromResult(RateLimitDecision.Allow());
        }

        var retryAfter = record.WindowExpiresAt - now;
        return ValueTask.FromResult(RateLimitDecision.Deny(retryAfter > TimeSpan.Zero ? retryAfter : TimeSpan.FromSeconds(1)));
    }
}
