namespace XanhNow.Security.Worker.Runtime;

public sealed record WorkerExecutionContext(
    string ServiceIdentity,
    string WorkerInstanceId,
    Guid JobRunId,
    string JobName,
    DateTimeOffset StartedAt);
