using XanhNow.Security.Domain.Common;
using XanhNow.Security.Domain.Operations.Events;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Domain.Operations;

public sealed class SecurityOperationRequest : AggregateRoot<Guid>
{
    private readonly List<OperationStepState> _steps = [];

    private SecurityOperationRequest()
    {
    }

    private SecurityOperationRequest(Guid id, Guid userId, OperationTypeCode operationType, IdempotencyKey idempotencyKey, DateTimeOffset createdAt, DateTimeOffset expiresAt, IEnumerable<OperationStepState> steps) : base(Guard.NotEmpty(id, nameof(id)))
    {
        Guard.NotEmpty(userId, nameof(userId));
        Guard.True(expiresAt > createdAt, "operation_expiry_invalid", "Operation expiry must be after creation time.");

        UserId = userId;
        OperationType = operationType;
        IdempotencyKey = idempotencyKey;
        Status = SecurityOperationStatus.Pending;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
        _steps.AddRange(steps);
        Guard.True(_steps.Count > 0, "operation_steps_required", "Operation requires at least one step.");
    }

    public Guid UserId { get; private set; }
    public OperationTypeCode OperationType { get; private set; } = null!;
    public IdempotencyKey IdempotencyKey { get; private set; } = null!;
    public SecurityOperationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? TerminalAt { get; private set; }
    public IReadOnlyCollection<OperationStepState> Steps => _steps.AsReadOnly();

    public static SecurityOperationRequest Create(Guid id, Guid userId, OperationTypeCode operationType, IdempotencyKey idempotencyKey, DateTimeOffset createdAt, DateTimeOffset expiresAt, IEnumerable<OperationStepState> steps)
        => new(id, userId, operationType, idempotencyKey, createdAt, expiresAt, steps);

    public void BeginValidation(DateTimeOffset at) => Move(SecurityOperationStatus.Validating, SecurityOperationStatus.Pending, at);
    public void Start(DateTimeOffset at) => Move(SecurityOperationStatus.Running, SecurityOperationStatus.Validating, at);

    public void MarkStepRunning(OperationTypeCode stepCode, DateTimeOffset at)
    {
        EnsureStatus(SecurityOperationStatus.Running);
        FindStep(stepCode).MarkRunning(at);
    }

    public void CompleteStep(OperationTypeCode stepCode, DateTimeOffset at)
    {
        EnsureStatus(SecurityOperationStatus.Running);
        FindStep(stepCode).Complete(at);
    }

    public void MarkPartial(DateTimeOffset at)
    {
        EnsureStatus(SecurityOperationStatus.Running);
        MoveTo(SecurityOperationStatus.Partial, at);
    }

    public void ScheduleRetry(OperationTypeCode stepCode, string failureCode, DateTimeOffset at)
    {
        EnsureStatus(SecurityOperationStatus.Partial);
        FindStep(stepCode).MarkRetryPending(failureCode);
        MoveTo(SecurityOperationStatus.RetryPending, at);
    }

    public void ResumeRetry(DateTimeOffset at) => Move(SecurityOperationStatus.Running, SecurityOperationStatus.RetryPending, at);

    public void Complete(DateTimeOffset at)
    {
        EnsureStatus(SecurityOperationStatus.Running);
        Guard.True(_steps.Where(step => step.Required).All(step => step.Status == OperationStepStatus.Completed), "operation_required_steps_incomplete", "Required operation steps must be completed.");
        MoveTo(SecurityOperationStatus.Completed, at);
        TerminalAt = at;
    }

    public void Cancel(DateTimeOffset at)
    {
        EnsureStatus(SecurityOperationStatus.Pending);
        MoveTo(SecurityOperationStatus.Cancelled, at);
        TerminalAt = at;
    }

    public void Expire(DateTimeOffset at)
    {
        EnsureNotTerminal();
        Guard.True(at >= ExpiresAt, "operation_not_expired", "Operation cannot expire before expiry time.");
        MoveTo(SecurityOperationStatus.Expired, at);
        TerminalAt = at;
    }

    public void FailSafe(string failureCode, DateTimeOffset at)
    {
        EnsureNotTerminal();
        foreach (var step in _steps.Where(step => step.Status != OperationStepStatus.Completed))
        {
            step.FailSafe(failureCode);
        }

        MoveTo(SecurityOperationStatus.FailedSafe, at);
        TerminalAt = at;
    }

    private OperationStepState FindStep(OperationTypeCode stepCode)
        => _steps.SingleOrDefault(step => step.StepCode == stepCode)
            ?? throw new DomainException("operation_step_not_found", $"Operation step '{stepCode.Value}' was not found.");

    private void Move(SecurityOperationStatus to, SecurityOperationStatus requiredFrom, DateTimeOffset at)
    {
        EnsureStatus(requiredFrom);
        MoveTo(to, at);
    }

    private void MoveTo(SecurityOperationStatus to, DateTimeOffset at)
    {
        var from = Status;
        Status = to;
        Raise(new OperationStatusChangedDomainEvent(Id, UserId, OperationType, from, to, at));
    }

    private void EnsureStatus(SecurityOperationStatus expected)
    {
        EnsureNotTerminal();
        if (Status != expected)
        {
            throw new DomainException("operation_transition_invalid", $"Cannot change operation from {Status} when expected status is {expected}.");
        }
    }

    private void EnsureNotTerminal()
    {
        if (Status is SecurityOperationStatus.Completed or SecurityOperationStatus.Cancelled or SecurityOperationStatus.Expired or SecurityOperationStatus.FailedSafe)
        {
            throw new DomainException("operation_terminal", "Operation is terminal.");
        }
    }
}
