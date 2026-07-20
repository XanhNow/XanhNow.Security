using XanhNow.Security.Domain.Common;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Domain.Operations;

public sealed class OperationStepState : Entity<Guid>
{
    private OperationStepState()
    {
    }

    private OperationStepState(Guid id, OperationTypeCode stepCode, bool required, DateTimeOffset createdAt) : base(Guard.NotEmpty(id, nameof(id)))
    {
        StepCode = stepCode;
        Required = required;
        Status = OperationStepStatus.Pending;
        CreatedAt = createdAt;
    }

    public OperationTypeCode StepCode { get; private set; } = null!;
    public bool Required { get; private set; }
    public OperationStepStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? FailureCode { get; private set; }

    public static OperationStepState Create(Guid id, OperationTypeCode stepCode, bool required, DateTimeOffset createdAt)
        => new(id, stepCode, required, createdAt);

    public void MarkRunning(DateTimeOffset at)
    {
        if (Status != OperationStepStatus.Pending && Status != OperationStepStatus.RetryPending)
        {
            throw new DomainException("operation_step_start_invalid", $"Cannot start operation step from {Status}.");
        }

        Status = OperationStepStatus.Running;
        StartedAt = at;
        FailureCode = null;
    }

    public void MarkRetryPending(string failureCode)
    {
        if (Status != OperationStepStatus.Running)
        {
            throw new DomainException("operation_step_retry_invalid", "Only running step can be scheduled for retry.");
        }

        Status = OperationStepStatus.RetryPending;
        FailureCode = Guard.NotBlank(failureCode, nameof(failureCode), 128);
    }

    public void Complete(DateTimeOffset at)
    {
        if (Status != OperationStepStatus.Running)
        {
            throw new DomainException("operation_step_complete_invalid", "Only running step can be completed.");
        }

        Status = OperationStepStatus.Completed;
        CompletedAt = at;
        FailureCode = null;
    }

    public void FailSafe(string failureCode)
    {
        if (Status == OperationStepStatus.Completed)
        {
            throw new DomainException("operation_step_completed", "Completed step cannot be failed safe.");
        }

        Status = OperationStepStatus.FailedSafe;
        FailureCode = Guard.NotBlank(failureCode, nameof(failureCode), 128);
    }
}
