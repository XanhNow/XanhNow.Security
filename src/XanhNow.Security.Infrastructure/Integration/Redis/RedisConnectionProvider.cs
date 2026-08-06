using StackExchange.Redis;

namespace XanhNow.Security.Infrastructure.Integration.Redis;

internal sealed class RedisConnectionProvider
{
    public RedisConnectionProvider(IConnectionMultiplexer? connection) => Connection = connection;

    public IConnectionMultiplexer? Connection { get; }
}
