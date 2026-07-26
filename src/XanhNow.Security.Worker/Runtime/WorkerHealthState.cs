namespace XanhNow.Security.Worker.Runtime;

public sealed class WorkerHealthState
{
    private readonly object _gate = new();
    private readonly Dictionary<string, WorkerJobHealthSnapshot> _jobs = new(StringComparer.Ordinal);

    public DateTimeOffset StartedAt { get; } = DateTimeOffset.UtcNow;
    public Exception? FatalException { get; private set; }

    public void RecordSuccess(string jobName, DateTimeOffset completedAt)
    {
        lock (_gate)
        {
            _jobs[jobName] = new WorkerJobHealthSnapshot(jobName, completedAt, null);
        }
    }

    public void RecordFailure(string jobName, DateTimeOffset failedAt, string errorCode)
    {
        lock (_gate)
        {
            _jobs[jobName] = new WorkerJobHealthSnapshot(jobName, failedAt, errorCode);
        }
    }

    public void RecordFatal(Exception exception)
    {
        lock (_gate)
        {
            FatalException = exception;
        }
    }

    public IReadOnlyCollection<WorkerJobHealthSnapshot> Snapshot()
    {
        lock (_gate)
        {
            return _jobs.Values.ToArray();
        }
    }
}

public sealed record WorkerJobHealthSnapshot(string JobName, DateTimeOffset LastObservedAt, string? LastErrorCode);
