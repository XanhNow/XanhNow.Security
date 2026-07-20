using System.Text.Json;
using XanhNow.Security.Application.Abstractions.Caching;
using XanhNow.Security.Infrastructure.Integration.Options;

namespace XanhNow.Security.Infrastructure.Integration.Redis;

internal sealed class RedisApplicationCache : IApplicationCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly RedisRuntimeState _state;
    private readonly RedisIntegrationOptions _options;

    public RedisApplicationCache(RedisRuntimeState state, SecurityIntegrationOptions options)
    {
        _state = state;
        _options = options.Redis;
    }

    public ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var namespacedKey = Namespaced(key);
        if (!_state.Values.TryGetValue(namespacedKey, out var record) || record.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _state.Values.TryRemove(namespacedKey, out _);
            return ValueTask.FromResult<T?>(default);
        }

        return ValueTask.FromResult(JsonSerializer.Deserialize<T>(record.ValueJson, JsonOptions));
    }

    public ValueTask SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var safeTtl = ttl > TimeSpan.Zero ? ttl : _options.DefaultCacheTtl;
        _state.Values[Namespaced(key)] = new RedisValueRecord(JsonSerializer.Serialize(value, JsonOptions), DateTimeOffset.UtcNow.Add(safeTtl));
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _state.Values.TryRemove(Namespaced(key), out _);
        return ValueTask.CompletedTask;
    }

    private string Namespaced(string key) => $"{_options.KeyPrefix}:cache:{key}";
}
