using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using XanhNow.Security.Infrastructure.Persistence;
using XanhNow.Security.Migrator.Options;
using XanhNow.Security.Migrator.Planning;
using XanhNow.Security.Migrator.Verification;

namespace XanhNow.Security.Migrator.Services;

public sealed class EfTargetPreflightService : ITargetPreflightService
{
    private readonly SecurityDbContext _dbContext;
    private readonly MigratorOptions _options;

    public EfTargetPreflightService(SecurityDbContext dbContext, IOptions<MigratorOptions> options)
    {
        _dbContext = dbContext;
        _options = options.Value;
    }

    public async Task<TargetPreflightResult> CheckAsync(CancellationToken cancellationToken)
    {
        var database = await _dbContext.Database.SqlQueryRaw<string>("SELECT current_database() AS \"Value\"").SingleAsync(cancellationToken).ConfigureAwait(false);
        var role = await _dbContext.Database.SqlQueryRaw<string>("SELECT current_user AS \"Value\"").SingleAsync(cancellationToken).ConfigureAwait(false);
        var schema = await _dbContext.Database.SqlQueryRaw<string>("SELECT current_schema() AS \"Value\"").SingleAsync(cancellationToken).ConfigureAwait(false);

        var expected = string.Equals(database, _options.ExpectedDatabase, StringComparison.Ordinal)
            && string.Equals(role, _options.ExpectedRole, StringComparison.Ordinal)
            && (string.Equals(schema, _options.ExpectedSchema, StringComparison.Ordinal) || string.Equals(schema, "public", StringComparison.Ordinal));

        return new TargetPreflightResult(database, role, schema, expected);
    }
}

public sealed class EfMigrationPlanner : IMigrationPlanner
{
    private readonly SecurityDbContext _dbContext;

    public EfMigrationPlanner(SecurityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MigrationPlan> CreatePlanAsync(CancellationToken cancellationToken)
    {
        var applied = await _dbContext.Database.GetAppliedMigrationsAsync(cancellationToken).ConfigureAwait(false);
        var pending = await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false);
        return new MigrationPlan(applied.ToArray(), pending.ToArray());
    }
}

public sealed class EfMigrationApplier : IMigrationApplier
{
    private readonly SecurityDbContext _dbContext;

    public EfMigrationApplier(SecurityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task ApplyAsync(CancellationToken cancellationToken)
    {
        return _dbContext.Database.MigrateAsync(cancellationToken);
    }
}

public sealed class EfMigrationVerifier : IMigrationVerifier
{
    private readonly SecurityDbContext _dbContext;

    public EfMigrationVerifier(SecurityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<MigrationVerificationResult> VerifyAsync(CancellationToken cancellationToken)
    {
        var pending = await _dbContext.Database.GetPendingMigrationsAsync(cancellationToken).ConfigureAwait(false);
        var tables = await _dbContext.Database
            .SqlQueryRaw<string>("SELECT table_name AS \"Value\" FROM information_schema.tables WHERE table_schema = {0}", SecurityDatabaseConstants.Schema)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        var existing = tables.ToHashSet(StringComparer.Ordinal);
        var missing = SecurityDatabaseConstants.ExpectedTables.Where(table => !existing.Contains(table)).ToArray();
        return new MigrationVerificationResult(pending.ToArray(), missing);
    }
}
