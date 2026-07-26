using Microsoft.EntityFrameworkCore;
using XanhNow.Security.Infrastructure.Persistence;

namespace XanhNow.Security.Migrator.Runtime;

public sealed class PostgresAdvisoryMigrationLockManager : IMigrationLockManager
{
    private readonly SecurityDbContext _dbContext;

    public PostgresAdvisoryMigrationLockManager(SecurityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IMigrationLock> TryAcquireAsync(string lockKey, CancellationToken cancellationToken)
    {
        var acquired = await _dbContext.Database
            .SqlQueryRaw<bool>("SELECT pg_try_advisory_lock(hashtext({0})) AS \"Value\"", lockKey)
            .SingleAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PostgresAdvisoryMigrationLock(_dbContext, lockKey, acquired);
    }
}

internal sealed class PostgresAdvisoryMigrationLock : IMigrationLock
{
    private readonly SecurityDbContext _dbContext;
    private readonly string _lockKey;

    public PostgresAdvisoryMigrationLock(SecurityDbContext dbContext, string lockKey, bool acquired)
    {
        _dbContext = dbContext;
        _lockKey = lockKey;
        Acquired = acquired;
    }

    public bool Acquired { get; }

    public async ValueTask DisposeAsync()
    {
        if (!Acquired)
        {
            return;
        }

        await _dbContext.Database
            .SqlQueryRaw<bool>("SELECT pg_advisory_unlock(hashtext({0})) AS \"Value\"", _lockKey)
            .SingleAsync()
            .ConfigureAwait(false);
    }
}
