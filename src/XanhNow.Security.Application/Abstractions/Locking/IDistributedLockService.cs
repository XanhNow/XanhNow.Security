namespace XanhNow.Security.Application.Abstractions.Locking;

public interface IDistributedLockHandle : IAsyncDisposable
{
    string Key { get; }
    string OwnerToken { get; }
}

public interface IDistributedLockService
{
    ValueTask<IDistributedLockHandle?> TryAcquireAsync(string key, TimeSpan ttl, CancellationToken cancellationToken);
}
