using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using XanhNow.Security.Migrator.Credentials;
using XanhNow.Security.Migrator.Options;
using XanhNow.Security.Migrator.Planning;
using XanhNow.Security.Migrator.Runtime;
using XanhNow.Security.Migrator.Services;
using XanhNow.Security.Migrator.Verification;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace XanhNow.Security.Migrator.Tests;

public sealed class MigrationRunnerTests
{
    [Fact]
    public async Task RunAsync_PlanMode_DoesNotApplyMigrations()
    {
        var applier = new FakeApplier();
        var runner = CreateRunner(applier: applier);

        var exitCode = await runner.RunAsync(MigratorMode.Plan, CancellationToken.None);

        Assert.Equal(MigratorExitCode.Success, exitCode);
        Assert.False(applier.Called);
    }

    [Fact]
    public async Task RunAsync_ApplyModeRequiresAllowApply()
    {
        var runner = CreateRunner(options: new MigratorOptions { AllowApply = false });

        var exitCode = await runner.RunAsync(MigratorMode.Apply, CancellationToken.None);

        Assert.Equal(MigratorExitCode.ConfigurationError, exitCode);
    }

    [Fact]
    public async Task RunAsync_ReturnsLockUnavailableWhenLockCannotBeAcquired()
    {
        var runner = CreateRunner(lockManager: new FakeLockManager(false));

        var exitCode = await runner.RunAsync(MigratorMode.Plan, CancellationToken.None);

        Assert.Equal(MigratorExitCode.LockUnavailable, exitCode);
    }

    [Fact]
    public async Task RunAsync_ReturnsVerificationFailedWhenExpectedTablesMissing()
    {
        var runner = CreateRunner(
            options: new MigratorOptions { AllowApply = true },
            verifier: new FakeVerifier(new MigrationVerificationResult([], ["security_users"])));

        var exitCode = await runner.RunAsync(MigratorMode.Apply, CancellationToken.None);

        Assert.Equal(MigratorExitCode.VerificationFailed, exitCode);
    }

    private static MigrationRunner CreateRunner(
        MigratorOptions? options = null,
        IMigrationApplier? applier = null,
        IMigrationVerifier? verifier = null,
        IMigrationLockManager? lockManager = null)
    {
        return new MigrationRunner(
            OptionsFactory.Create(options ?? new MigratorOptions()),
            new FakeCredentialProvider(),
            new FakePreflight(),
            new FakePlanner(),
            applier ?? new FakeApplier(),
            verifier ?? new FakeVerifier(new MigrationVerificationResult([], [])),
            lockManager ?? new FakeLockManager(true),
            NullLogger<MigrationRunner>.Instance);
    }

    private sealed class FakeCredentialProvider : IMigratorCredentialProvider
    {
        public Task<string> LoadConnectionStringAsync(CancellationToken cancellationToken) => Task.FromResult("Host=localhost;Database=authtest;Username=s101_xanhnow_auth_security_migrator;Password=secret");
    }

    private sealed class FakePreflight : ITargetPreflightService
    {
        public Task<TargetPreflightResult> CheckAsync(CancellationToken cancellationToken) => Task.FromResult(new TargetPreflightResult("authtest", "s101_xanhnow_auth_security_migrator", "security", true));
    }

    private sealed class FakePlanner : IMigrationPlanner
    {
        public Task<MigrationPlan> CreatePlanAsync(CancellationToken cancellationToken) => Task.FromResult(new MigrationPlan([], ["InitialSecuritySchema"]));
    }

    private sealed class FakeApplier : IMigrationApplier
    {
        public bool Called { get; private set; }
        public Task ApplyAsync(CancellationToken cancellationToken)
        {
            Called = true;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeVerifier : IMigrationVerifier
    {
        private readonly MigrationVerificationResult _result;
        public FakeVerifier(MigrationVerificationResult result) => _result = result;
        public Task<MigrationVerificationResult> VerifyAsync(CancellationToken cancellationToken) => Task.FromResult(_result);
    }

    private sealed class FakeLockManager : IMigrationLockManager
    {
        private readonly bool _acquired;
        public FakeLockManager(bool acquired) => _acquired = acquired;
        public Task<IMigrationLock> TryAcquireAsync(string lockKey, CancellationToken cancellationToken) => Task.FromResult<IMigrationLock>(new FakeLock(_acquired));
    }

    private sealed class FakeLock : IMigrationLock
    {
        public FakeLock(bool acquired) => Acquired = acquired;
        public bool Acquired { get; }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
