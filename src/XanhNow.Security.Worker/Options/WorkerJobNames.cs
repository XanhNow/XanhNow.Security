namespace XanhNow.Security.Worker.Options;

public static class WorkerJobNames
{
    public const string OutboxDispatcher = "outbox-dispatcher";
    public const string OutboxRetry = "outbox-retry";
    public const string DeadLetterMonitor = "dead-letter-monitor";
    public const string OutboxCleanup = "outbox-cleanup";
    public const string OperationRetry = "operation-retry";
    public const string RecoveryResume = "recovery-resume";
    public const string GrantExpiry = "grant-expiry";
    public const string ExpiredOperation = "expired-operation";
    public const string RetentionCleanup = "retention-cleanup";
    public const string PolicyCacheRefresh = "policy-cache-refresh";
    public const string ProjectionRefresh = "projection-refresh";
}
