using XanhNow.Security.Application.Abstractions.Time;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Application.Common.Results;

namespace XanhNow.Security.Application.Background.Commands;

public sealed class RunBackgroundJobCommandHandler : IRequestHandler<RunBackgroundJobCommand, BackgroundCommandResult>
{
    private static readonly HashSet<string> SupportedFoundationJobs = new(StringComparer.Ordinal)
    {
        "outbox-dispatcher",
        "outbox-retry",
        "dead-letter-monitor",
        "outbox-cleanup",
        "operation-retry",
        "recovery-resume",
        "grant-expiry",
        "expired-operation",
        "retention-cleanup",
        "policy-cache-refresh",
        "projection-refresh"
    };

    private readonly IClock _clock;

    public RunBackgroundJobCommandHandler(IClock clock)
    {
        _clock = clock;
    }

    public Task<Result<BackgroundCommandResult>> HandleAsync(RunBackgroundJobCommand request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(request.WorkerInstanceId))
        {
            return Task.FromResult(Result<BackgroundCommandResult>.Failure(Error.Validation(
                "SECURITY_WORKER_INSTANCE_REQUIRED",
                "Worker instance id is required.")));
        }

        if (!SupportedFoundationJobs.Contains(request.JobName))
        {
            return Task.FromResult(Result<BackgroundCommandResult>.Failure(Error.Validation(
                "SECURITY_WORKER_JOB_UNSUPPORTED",
                $"Worker job '{request.JobName}' is not registered.")));
        }

        var result = BackgroundCommandResult.NoWork(request.JobName, _clock.UtcNow);
        return Task.FromResult(Result<BackgroundCommandResult>.Success(result));
    }
}
