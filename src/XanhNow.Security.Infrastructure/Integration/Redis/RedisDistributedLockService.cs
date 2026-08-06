using StackExchange.Redis;
using XanhNow.Security.Application.Abstractions.Locking;
using XanhNow.Security.Infrastructure.Integration.Options;

namespace XanhNow.Security.Infrastructure.Integration.Redis;

internal sealed class RedisDistributedLockService : IDistributedLockService
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly RedisRuntimeState _state;
    private readonly RedisIntegrationOptions _options;

    public RedisDistributedLockService(RedisConnectionProvider redis, RedisRuntimeState state, SecurityIntegrationOptions options)
    {
        _redis = redis.Connection;
        _state = state;
        _options = options.Redis;
    }

    public async ValueTask<IDistributedLockHandle?> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var safeTtl = ttl > TimeSpan.Zero ? ttl : _options.LockTtl;
        var namespacedKey = $"{_options.KeyPrefix}:lock:{key}";
        var ownerToken = Guid.NewGuid().ToString("N");

        if (_redis is not null)
        {
            var acquired = await _redis.GetDatabase().StringSetAsync(namespacedKey, ownerToken, safeTtl, When.NotExists);
            return acquired ? new RedisDistributedLockHandle(_redis, namespacedKey, key, ownerToken) : null;
        }

        var now = DateTimeOffset.UtcNow;
        var expiresAt = now.Add(safeTtl);

        while (true)
        {
            if (!_state.Locks.TryGetValue(namespacedKey, out var current))
            {
                if (_state.Locks.TryAdd(namespacedKey, new RedisLockRecord(ownerToken, expiresAt)))
                {
                    return new InMemoryDistributedLockHandle(_state, namespacedKey, key, ownerToken);
                }

                continue;
            }

            if (current.ExpiresAt <= now)
            {
                if (_state.Locks.TryUpdate(namespacedKey, new RedisLockRecord(ownerToken, expiresAt), current))
                {
                    return new InMemoryDistributedLockHandle(_state, namespacedKey, key, ownerToken);
                }

                continue;
            }

            return null;
        }
    }

    private sealed class RedisDistributedLockHandle : IDistributedLockHandle
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly string _namespacedKey;

        public RedisDistributedLockHandle(IConnectionMultiplexer redis, string namespacedKey, string key, string ownerToken)
        {
            _redis = redis;
            _namespacedKey = namespacedKey;
            Key = key;
            OwnerToken = ownerToken;
        }

        public string Key { get; }
        public string OwnerToken { get; }

        public async ValueTask DisposeAsync()
        {
            var db = _redis.GetDatabase();
            var current = await db.StringGetAsync(_namespacedKey);
            if (current.HasValue && string.Equals(current!, OwnerToken, StringComparison.Ordinal))
            {
                await db.KeyDeleteAsync(_namespacedKey);
            }
        }
    }

    private sealed class InMemoryDistributedLockHandle : IDistributedLockHandle
    {
        private readonly RedisRuntimeState _state;
        private readonly string _namespacedKey;

        public InMemoryDistributedLockHandle(RedisRuntimeState state, string namespacedKey, string key, string ownerToken)
        {
            _state = state;
            _namespacedKey = namespacedKey;
            Key = key;
            OwnerToken = ownerToken;
        }

        public string Key { get; }
        public string OwnerToken { get; }

        public ValueTask DisposeAsync()
        {
            if (_state.Locks.TryGetValue(_namespacedKey, out var current) && current.OwnerToken == OwnerToken)
            {
                _state.Locks.TryRemove(_namespacedKey, out _);
            }

            return ValueTask.CompletedTask;
        }
    }
}
