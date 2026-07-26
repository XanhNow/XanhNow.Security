using XanhNow.Security.Application.Background;
using XanhNow.Security.Application.Background.Commands;
using XanhNow.Security.Application.Common.Requests;
using XanhNow.Security.Worker.Options;
using XanhNow.Security.Worker.Runtime;
using XanhNow.Security.Worker.Scheduling;

namespace XanhNow.Security.Worker.Jobs;

public sealed class ApplicationBackedWorkerJob : IWorkerJob
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IWorkerInstanceIdProvider _instanceIdProvider;
    private readonly WorkerHealthState _healthState;
    private readonly WorkerJobOptions _jobOptions;
    private readonly ILogger<ApplicationBackedWorkerJob> _logger;
    private int _running;

    public ApplicationBackedWorkerJob(
        WorkerJobOptions jobOptions,
        IServiceScopeFactory scopeFactory,
        IWorkerInstanceIdProvider instanceIdProvider,
        WorkerHealthState healthState,
        ILogger<ApplicationBackedWorkerJob> logger)
    {
        _jobOptions = jobOptions;
        _scopeFactory = scopeFactory;
        _instanceIdProvider = instanceIdProvider;
        _healthState = healthState;
        _logger = logger;
    }

    public string Name => _jobOptions.Name;

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _running, 1) == 1)
        {
            _logger.LogWarning("Skipping overlapping worker cycle for {JobName}.", Name);
            return;
        }

        var startedAt = DateTimeOffset.UtcNow;
        var jobRunId = Guid.NewGuid();

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var executor = scope.ServiceProvider.GetRequiredService<ApplicationExecutor<RunBackgroundJobCommand, BackgroundCommandResult>>();
            var command = new RunBackgroundJobCommand(Name, _instanceIdProvider.InstanceId, jobRunId, _jobOptions.BatchSize, startedAt);
            var result = await executor.ExecuteAsync(command, cancellationToken);

            if (result.IsFailure)
            {
                var code = result.Error?.Code ?? "SECURITY_WORKER_JOB_FAILED";
                _healthState.RecordFailure(Name, DateTimeOffset.UtcNow, code);
                _logger.LogWarning("Worker job {JobName} failed with {ErrorCode}.", Name, code);
                return;
            }

            _healthState.RecordSuccess(Name, result.Value?.CompletedAt ?? DateTimeOffset.UtcNow);
            _logger.LogInformation(
                "Worker job {JobName} completed. selected={Selected} processed={Processed} outcome={Outcome} workerInstanceId={WorkerInstanceId} jobRunId={JobRunId}",
                Name,
                result.Value?.Selected ?? 0,
                result.Value?.Processed ?? 0,
                result.Value?.Outcome ?? "unknown",
                _instanceIdProvider.InstanceId,
                jobRunId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Worker job {JobName} cancelled.", Name);
        }
        catch (Exception exception)
        {
            _healthState.RecordFailure(Name, DateTimeOffset.UtcNow, "SECURITY_WORKER_UNHANDLED_EXCEPTION");
            _healthState.RecordFatal(exception);
            _logger.LogError(exception, "Worker job {JobName} failed unexpectedly.", Name);
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
        }
    }
}
