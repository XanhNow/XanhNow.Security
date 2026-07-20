using XanhNow.Security.Application.Abstractions.Idempotency;
using XanhNow.Security.Infrastructure.Integration.Options;

namespace XanhNow.Security.Infrastructure.Integration.Redis;

internal sealed class RedisIdempotencyStore : IIdempotencyStore
{
    private readonly RedisRuntimeState _state;
    private readonly RedisIntegrationOptions _options;

    public RedisIdempotencyStore(RedisRuntimeState state, SecurityIntegrationOptions options)
    {
        _state = state;
        _options = options.Redis;
    }

    public ValueTask<IdempotencyRecord?> FindAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var namespacedKey = Namespaced(key);
        if (!_state.Values.TryGetValue(namespacedKey, out var record) || record.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _state.Values.TryRemove(namespacedKey, out _);
            return ValueTask.FromResult<IdempotencyRecord?>(null);
        }

        return ValueTask.FromResult<IdempotencyRecord?>(new IdempotencyRecord(key, record.RequestHash ?? string.Empty, record.ValueJson, record.Completed));
    }

    public ValueTask ReserveAsync(string key, string requestHash, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _state.Values.TryAdd(Namespaced(key), new RedisValueRecord(string.Empty, DateTimeOffset.UtcNow.Add(_options.IdempotencyTtl), requestHash, false));
        return ValueTask.CompletedTask;
    }

    public ValueTask CompleteAsync(string key, string resultJson, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var namespacedKey = Namespaced(key);
        _state.Values.AddOrUpdate(
            namespacedKey,
            _ => new RedisValueRecord(resultJson, DateTimeOffset.UtcNow.Add(_options.IdempotencyTtl), null, true),
            (_, current) => current with { ValueJson = resultJson, Completed = true });
        return ValueTask.CompletedTask;
    }

    private string Namespaced(string key) => $"{_options.KeyPrefix}:idem:{key}";
}
