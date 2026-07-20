using XanhNow.Security.Application.Abstractions.Locking;
using XanhNow.Security.Infrastructure.Integration.Options;

namespace XanhNow.Security.Infrastructure.Integration.Redis;

internal sealed class RedisDistributedLockService : IDistributedLockService
{
    private readonly RedisRuntimeState _state;
    private readonly RedisIntegrationOptions _options;

    public RedisDistributedLockService(RedisRuntimeState state, SecurityIntegrationOptions options)
    {
        _state = state;
        _options = options.Redis;
    }

    public ValueTask<IDistributedLockHandle?> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var namespacedKey = $"{_options.KeyPrefix}:lock:{key}";
        var now = DateTimeOffset.UtcNow;
        var ownerToken = Guid.NewGuid().ToString("N");
        var expiresAt = now.Add(ttl > TimeSpan.Zero ? ttl : _options.LockTtl);

        while (true)
        {
            if (!_state.Locks.TryGetValue(namespacedKey, out var current))
            {
                if (_state.Locks.TryAdd(namespacedKey, new RedisLockRecord(ownerToken, expiresAt)))
                {
                    return ValueTask.FromResult<IDistributedLockHandle?>(new RedisDistributedLockHandle(_state, namespacedKey, key, ownerToken));
                }

                continue;
            }

            if (current.ExpiresAt <= now)
            {
                if (_state.Locks.TryUpdate(namespacedKey, new RedisLockRecord(ownerToken, expiresAt), current))
                {
                    return ValueTask.FromResult<IDistributedLockHandle?>(new RedisDistributedLockHandle(_state, namespacedKey, key, ownerToken));
                }

                continue;
            }

            return ValueTask.FromResult<IDistributedLockHandle?>(null);
        }
    }

    private sealed class RedisDistributedLockHandle : IDistributedLockHandle
    {
        private readonly RedisRuntimeState _state;
        private readonly string _namespacedKey;

        public RedisDistributedLockHandle(RedisRuntimeState state, string namespacedKey, string key, string ownerToken)
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
