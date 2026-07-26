namespace XanhNow.Security.Worker.Options;

public sealed class SecurityWorkerOptions
{
    public const string SectionName = "SecurityWorker";

    public string ServiceIdentity { get; set; } = "xanhnow-security-worker";
    public TimeSpan ShutdownTimeout { get; set; } = TimeSpan.FromSeconds(30);
    public WorkerJobOptions OutboxDispatcher { get; set; } = new() { Name = WorkerJobNames.OutboxDispatcher, Enabled = true };
    public WorkerJobOptions OutboxRetry { get; set; } = new() { Name = WorkerJobNames.OutboxRetry };
    public WorkerJobOptions DeadLetterMonitor { get; set; } = new() { Name = WorkerJobNames.DeadLetterMonitor };
    public WorkerJobOptions OutboxCleanup { get; set; } = new() { Name = WorkerJobNames.OutboxCleanup };
    public WorkerJobOptions OperationRetry { get; set; } = new() { Name = WorkerJobNames.OperationRetry };
    public WorkerJobOptions RecoveryResume { get; set; } = new() { Name = WorkerJobNames.RecoveryResume };
    public WorkerJobOptions GrantExpiry { get; set; } = new() { Name = WorkerJobNames.GrantExpiry };
    public WorkerJobOptions ExpiredOperation { get; set; } = new() { Name = WorkerJobNames.ExpiredOperation };
    public WorkerJobOptions RetentionCleanup { get; set; } = new() { Name = WorkerJobNames.RetentionCleanup };
    public WorkerJobOptions PolicyCacheRefresh { get; set; } = new() { Name = WorkerJobNames.PolicyCacheRefresh };
    public WorkerJobOptions ProjectionRefresh { get; set; } = new() { Name = WorkerJobNames.ProjectionRefresh };

    public IReadOnlyList<WorkerJobOptions> AllJobs() =>
    [
        OutboxDispatcher,
        OutboxRetry,
        DeadLetterMonitor,
        OutboxCleanup,
        OperationRetry,
        RecoveryResume,
        GrantExpiry,
        ExpiredOperation,
        RetentionCleanup,
        PolicyCacheRefresh,
        ProjectionRefresh
    ];
}
