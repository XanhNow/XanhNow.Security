namespace XanhNow.Security.Domain.Operations;

public enum SecurityOperationStatus
{
    Pending,
    Validating,
    Running,
    Partial,
    RetryPending,
    Completed,
    FailedSafe,
    Cancelled,
    Expired
}
