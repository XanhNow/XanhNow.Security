using XanhNow.Security.Migrator.Planning;
using XanhNow.Security.Migrator.Verification;

namespace XanhNow.Security.Migrator.Services;

public interface ITargetPreflightService
{
    Task<TargetPreflightResult> CheckAsync(CancellationToken cancellationToken);
}

public interface IMigrationPlanner
{
    Task<MigrationPlan> CreatePlanAsync(CancellationToken cancellationToken);
}

public interface IMigrationApplier
{
    Task ApplyAsync(CancellationToken cancellationToken);
}

public interface IMigrationVerifier
{
    Task<MigrationVerificationResult> VerifyAsync(CancellationToken cancellationToken);
}
