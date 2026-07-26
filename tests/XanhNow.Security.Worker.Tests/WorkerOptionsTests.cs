using XanhNow.Security.Worker.Options;

namespace XanhNow.Security.Worker.Tests;

public sealed class WorkerOptionsTests
{
    [Fact]
    public void Validator_accepts_default_worker_foundation_options()
    {
        var result = new SecurityWorkerOptionsValidator().Validate(null, new SecurityWorkerOptions());

        Assert.False(result.Failed);
    }

    [Fact]
    public void Validator_rejects_invalid_enabled_job_limits()
    {
        var options = new SecurityWorkerOptions
        {
            OutboxDispatcher = new WorkerJobOptions
            {
                Name = WorkerJobNames.OutboxDispatcher,
                Enabled = true,
                Interval = TimeSpan.Zero,
                BatchSize = 0,
                MaxConcurrency = 0,
                Lease = TimeSpan.Zero,
                MaxAttempts = 0,
                BaseDelay = TimeSpan.FromSeconds(10),
                MaxDelay = TimeSpan.FromSeconds(1),
                JitterPercent = 99
            }
        };

        var result = new SecurityWorkerOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures, failure => failure.Contains("interval", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Failures, failure => failure.Contains("batch", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Failures, failure => failure.Contains("concurrency", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Failures, failure => failure.Contains("lease", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Failures, failure => failure.Contains("attempt", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(result.Failures, failure => failure.Contains("jitter", StringComparison.OrdinalIgnoreCase));
    }
}
