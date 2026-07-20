namespace XanhNow.Security.Domain.Operations;

public enum OperationStepStatus
{
    Pending,
    Running,
    RetryPending,
    Completed,
    FailedSafe
}
