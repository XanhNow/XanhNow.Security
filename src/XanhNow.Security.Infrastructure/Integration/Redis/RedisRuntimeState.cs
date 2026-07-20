using System.Collections.Concurrent;

namespace XanhNow.Security.Infrastructure.Integration.Redis;

internal sealed class RedisRuntimeState
{
    public ConcurrentDictionary<string, RedisValueRecord> Values { get; } = new(StringComparer.Ordinal);
    public ConcurrentDictionary<string, RedisCounterRecord> Counters { get; } = new(StringComparer.Ordinal);
    public ConcurrentDictionary<string, RedisLockRecord> Locks { get; } = new(StringComparer.Ordinal);
}

internal sealed record RedisValueRecord(string ValueJson, DateTimeOffset ExpiresAt, string? RequestHash = null, bool Completed = false);
internal sealed record RedisCounterRecord(int Count, DateTimeOffset WindowExpiresAt);
internal sealed record RedisLockRecord(string OwnerToken, DateTimeOffset ExpiresAt);
