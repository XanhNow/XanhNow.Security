using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using XanhNow.Security.Migrator.Credentials;
using XanhNow.Security.Migrator.Options;
using XanhNow.Security.Migrator.Runtime;
using XanhNow.Security.Migrator.Services;

namespace XanhNow.Security.Migrator;

public sealed class MigrationRunner
{
    private readonly MigratorOptions _options;
    private readonly IMigratorCredentialProvider _credentialProvider;
    private readonly ITargetPreflightService _preflight;
    private readonly IMigrationPlanner _planner;
    private readonly IMigrationApplier _applier;
    private readonly IMigrationVerifier _verifier;
    private readonly IMigrationLockManager _lockManager;
    private readonly ILogger<MigrationRunner> _logger;

    public MigrationRunner(
        IOptions<MigratorOptions> options,
        IMigratorCredentialProvider credentialProvider,
        ITargetPreflightService preflight,
        IMigrationPlanner planner,
        IMigrationApplier applier,
        IMigrationVerifier verifier,
        IMigrationLockManager lockManager,
        ILogger<MigrationRunner> logger)
    {
        _options = options.Value;
        _credentialProvider = credentialProvider;
        _preflight = preflight;
        _planner = planner;
        _applier = applier;
        _verifier = verifier;
        _lockManager = lockManager;
        _logger = logger;
    }

    public async Task<MigratorExitCode> RunAsync(MigratorMode mode, CancellationToken cancellationToken)
    {
        try
        {
            await _credentialProvider.LoadConnectionStringAsync(cancellationToken).ConfigureAwait(false);

            await using var migrationLock = await _lockManager.TryAcquireAsync(_options.LockKey, cancellationToken).ConfigureAwait(false);
            if (!migrationLock.Acquired)
            {
                _logger.LogError("Security migrator lock is already held.");
                return MigratorExitCode.LockUnavailable;
            }

            var preflight = await _preflight.CheckAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Security migrator preflight: {Detail}", preflight.Detail);
            if (!preflight.IsExpected)
            {
                _logger.LogError("Security migrator preflight failed.");
                return MigratorExitCode.PreflightFailed;
            }

            var plan = await _planner.CreatePlanAsync(cancellationToken).ConfigureAwait(false);
            _logger.LogInformation("Security migrator plan: applied={AppliedCount}; pending={PendingCount}", plan.AppliedMigrations.Count, plan.PendingMigrations.Count);

            if (mode == MigratorMode.Validate || mode == MigratorMode.Plan)
            {
                return MigratorExitCode.Success;
            }

            if (!_options.AllowApply)
            {
                _logger.LogError("Security migrator apply mode is disabled by configuration.");
                return MigratorExitCode.ConfigurationError;
            }

            if (plan.HasPendingMigrations)
            {
                await _applier.ApplyAsync(cancellationToken).ConfigureAwait(false);
            }

            var verification = await _verifier.VerifyAsync(cancellationToken).ConfigureAwait(false);
            if (!verification.IsValid)
            {
                _logger.LogError("Security migrator verification failed: pending={PendingCount}; missingTables={MissingCount}", verification.PendingMigrations.Count, verification.MissingTables.Count);
                return MigratorExitCode.VerificationFailed;
            }

            return MigratorExitCode.Success;
        }
        catch (MigratorCredentialException ex)
        {
            _logger.LogError("Security migrator credential unavailable: {Reason}", ex.Message);
            return MigratorExitCode.CredentialUnavailable;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Security migrator cancelled.");
            return MigratorExitCode.Cancelled;
        }
        catch (Exception ex) when (ex is Npgsql.PostgresException or System.Data.Common.DbException)
        {
            _logger.LogError("Security migrator database operation failed: {ErrorType}", ex.GetType().Name);
            return MigratorExitCode.MigrationFailed;
        }
        catch (Exception ex)
        {
            _logger.LogError("Security migrator unexpected failure: {ErrorType}", ex.GetType().Name);
            return MigratorExitCode.UnexpectedFailure;
        }
    }
}
