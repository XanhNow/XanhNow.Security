using StackExchange.Redis;
using XanhNow.Security.Application.Abstractions.Idempotency;
using XanhNow.Security.Infrastructure.Integration.Options;

namespace XanhNow.Security.Infrastructure.Integration.Redis;

internal sealed class RedisIdempotencyStore : IIdempotencyStore
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly RedisRuntimeState _state;
    private readonly RedisIntegrationOptions _options;

    public RedisIdempotencyStore(RedisConnectionProvider redis, RedisRuntimeState state, SecurityIntegrationOptions options)
    {
        _redis = redis.Connection;
        _state = state;
        _options = options.Redis;
    }

    public async ValueTask<IdempotencyRecord?> FindAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var namespacedKey = Namespaced(key);

        if (_redis is not null)
        {
            var entries = await _redis.GetDatabase().HashGetAllAsync(namespacedKey);
            if (entries.Length == 0)
            {
                return null;
            }

            var map = entries.ToDictionary(entry => entry.Name.ToString(), entry => entry.Value.ToString(), StringComparer.Ordinal);
            return new IdempotencyRecord(
                key,
                map.TryGetValue("requestHash", out var requestHash) ? requestHash : string.Empty,
                map.TryGetValue("resultJson", out var resultJson) ? resultJson : null,
                map.TryGetValue("completed", out var completed) && string.Equals(completed, "true", StringComparison.OrdinalIgnoreCase));
        }

        if (!_state.Values.TryGetValue(namespacedKey, out var record) || record.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _state.Values.TryRemove(namespacedKey, out _);
            return null;
        }

        return new IdempotencyRecord(key, record.RequestHash ?? string.Empty, record.ValueJson, record.Completed);
    }

    public async ValueTask ReserveAsync(string key, string requestHash, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var namespacedKey = Namespaced(key);

        if (_redis is not null)
        {
            var db = _redis.GetDatabase();
            await db.HashSetAsync(namespacedKey, [new HashEntry("requestHash", requestHash), new HashEntry("completed", "false")]);
            await db.KeyExpireAsync(namespacedKey, _options.IdempotencyTtl);
            return;
        }

        _state.Values.TryAdd(namespacedKey, new RedisValueRecord(string.Empty, DateTimeOffset.UtcNow.Add(_options.IdempotencyTtl), requestHash, false));
    }

    public async ValueTask CompleteAsync(string key, string resultJson, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var namespacedKey = Namespaced(key);

        if (_redis is not null)
        {
            var db = _redis.GetDatabase();
            await db.HashSetAsync(namespacedKey, [new HashEntry("resultJson", resultJson), new HashEntry("completed", "true")]);
            await db.KeyExpireAsync(namespacedKey, _options.IdempotencyTtl);
            return;
        }

        _state.Values.AddOrUpdate(
            namespacedKey,
            _ => new RedisValueRecord(resultJson, DateTimeOffset.UtcNow.Add(_options.IdempotencyTtl), null, true),
            (_, current) => current with { ValueJson = resultJson, Completed = true });
    }

    private string Namespaced(string key) => $"{_options.KeyPrefix}:idem:{key}";
}
