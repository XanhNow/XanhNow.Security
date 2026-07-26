namespace XanhNow.Security.Migrator.Runtime;

public interface IMigrationLock : IAsyncDisposable
{
    bool Acquired { get; }
}

public interface IMigrationLockManager
{
    Task<IMigrationLock> TryAcquireAsync(string lockKey, CancellationToken cancellationToken);
}
