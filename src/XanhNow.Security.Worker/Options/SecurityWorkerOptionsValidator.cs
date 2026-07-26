using Microsoft.Extensions.Options;

namespace XanhNow.Security.Worker.Options;

public sealed class SecurityWorkerOptionsValidator : IValidateOptions<SecurityWorkerOptions>
{
    private const int MaxBatchSize = 500;
    private const int MaxConcurrency = 32;

    public ValidateOptionsResult Validate(string? name, SecurityWorkerOptions options)
    {
        var failures = new List<string>();

        if (string.IsNullOrWhiteSpace(options.ServiceIdentity))
        {
            failures.Add("SecurityWorker:ServiceIdentity is required.");
        }

        if (options.ShutdownTimeout <= TimeSpan.Zero)
        {
            failures.Add("SecurityWorker:ShutdownTimeout must be greater than zero.");
        }

        foreach (var job in options.AllJobs())
        {
            ValidateJob(job, failures);
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateJob(WorkerJobOptions job, List<string> failures)
    {
        if (string.IsNullOrWhiteSpace(job.Name))
        {
            failures.Add("Worker job name is required.");
            return;
        }

        if (!job.Enabled)
        {
            return;
        }

        if (job.Interval <= TimeSpan.Zero)
        {
            failures.Add($"{job.Name}: interval must be greater than zero.");
        }

        if (job.BatchSize is < 1 or > MaxBatchSize)
        {
            failures.Add($"{job.Name}: batch size must be between 1 and {MaxBatchSize}.");
        }

        if (job.MaxConcurrency is < 1 or > MaxConcurrency)
        {
            failures.Add($"{job.Name}: max concurrency must be between 1 and {MaxConcurrency}.");
        }

        if (job.Lease <= TimeSpan.Zero)
        {
            failures.Add($"{job.Name}: lease must be greater than zero.");
        }

        if (job.MaxAttempts < 1)
        {
            failures.Add($"{job.Name}: max attempts must be at least 1.");
        }

        if (job.BaseDelay <= TimeSpan.Zero || job.MaxDelay < job.BaseDelay)
        {
            failures.Add($"{job.Name}: retry delay window is invalid.");
        }

        if (job.JitterPercent is < 0 or > 50)
        {
            failures.Add($"{job.Name}: jitter percent must be between 0 and 50.");
        }
    }
}
