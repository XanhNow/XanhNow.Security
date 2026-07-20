namespace XanhNow.Security.Application.Abstractions.Caching;

public interface IApplicationCache
{
    ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken);
    ValueTask SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken);
    ValueTask RemoveAsync(string key, CancellationToken cancellationToken);
}
