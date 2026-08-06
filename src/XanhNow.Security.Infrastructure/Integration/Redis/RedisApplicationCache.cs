using System.Text.Json;
using StackExchange.Redis;
using XanhNow.Security.Application.Abstractions.Caching;
using XanhNow.Security.Infrastructure.Integration.Options;

namespace XanhNow.Security.Infrastructure.Integration.Redis;

internal sealed class RedisApplicationCache : IApplicationCache
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IConnectionMultiplexer? _redis;
    private readonly RedisRuntimeState _state;
    private readonly RedisIntegrationOptions _options;

    public RedisApplicationCache(RedisConnectionProvider redis, RedisRuntimeState state, SecurityIntegrationOptions options)
    {
        _redis = redis.Connection;
        _state = state;
        _options = options.Redis;
    }

    public async ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var namespacedKey = Namespaced(key);

        if (_redis is not null)
        {
            var value = await _redis.GetDatabase().StringGetAsync(namespacedKey);
            return value.HasValue ? JsonSerializer.Deserialize<T>(value.ToString(), JsonOptions) : default;
        }

        if (!_state.Values.TryGetValue(namespacedKey, out var record) || record.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            _state.Values.TryRemove(namespacedKey, out _);
            return default;
        }

        return JsonSerializer.Deserialize<T>(record.ValueJson, JsonOptions);
    }

    public async ValueTask SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var safeTtl = ttl > TimeSpan.Zero ? ttl : _options.DefaultCacheTtl;
        var payload = JsonSerializer.Serialize(value, JsonOptions);
        var namespacedKey = Namespaced(key);

        if (_redis is not null)
        {
            await _redis.GetDatabase().StringSetAsync(namespacedKey, payload, safeTtl);
            return;
        }

        _state.Values[namespacedKey] = new RedisValueRecord(payload, DateTimeOffset.UtcNow.Add(safeTtl));
    }

    public async ValueTask RemoveAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var namespacedKey = Namespaced(key);

        if (_redis is not null)
        {
            await _redis.GetDatabase().KeyDeleteAsync(namespacedKey);
            return;
        }

        _state.Values.TryRemove(namespacedKey, out _);
    }

    private string Namespaced(string key) => $"{_options.KeyPrefix}:cache:{key}";
}
