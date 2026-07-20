using XanhNow.Security.Domain.Common;
using XanhNow.Security.Domain.Recovery.Events;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Domain.Recovery;

public sealed class SecurityRecoveryCase : AggregateRoot<Guid>
{
    private SecurityRecoveryCase()
    {
    }

    private SecurityRecoveryCase(Guid id, Guid userId, ReasonCode reason, DateTimeOffset createdAt) : base(Guard.NotEmpty(id, nameof(id)))
    {
        Guard.NotEmpty(userId, nameof(userId));
        UserId = userId;
        Reason = reason;
        Status = RecoveryCaseStatus.Pending;
        CreatedAt = createdAt;
    }

    public Guid UserId { get; private set; }
    public RecoveryCaseStatus Status { get; private set; }
    public ReasonCode Reason { get; private set; } = null!;
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? TerminalAt { get; private set; }

    public static SecurityRecoveryCase Open(Guid id, Guid userId, ReasonCode reason, DateTimeOffset createdAt) => new(id, userId, reason, createdAt);

    public void BeginProofVerification(DateTimeOffset at) => Move(RecoveryCaseStatus.VerifyingProof, RecoveryCaseStatus.Pending, at);
    public void ProtectAccount(DateTimeOffset at) => Move(RecoveryCaseStatus.ProtectingAccount, RecoveryCaseStatus.VerifyingProof, at);
    public void RevokeSessions(DateTimeOffset at) => Move(RecoveryCaseStatus.RevokingSessions, RecoveryCaseStatus.ProtectingAccount, at);
    public void DisableAuthenticators(DateTimeOffset at) => Move(RecoveryCaseStatus.DisablingAuthenticators, RecoveryCaseStatus.RevokingSessions, at);
    public void RestoreAccess(DateTimeOffset at) => Move(RecoveryCaseStatus.RestoringAccess, RecoveryCaseStatus.DisablingAuthenticators, at);
    public void Complete(DateTimeOffset at) => Move(RecoveryCaseStatus.Completed, RecoveryCaseStatus.RestoringAccess, at);

    public void Cancel(ReasonCode reason, DateTimeOffset at)
    {
        EnsureNotTerminal();
        var from = Status;
        Reason = reason;
        Status = RecoveryCaseStatus.Cancelled;
        TerminalAt = at;
        Raise(new RecoveryCaseStatusChangedDomainEvent(Id, UserId, from, Status, reason, at));
    }

    private void Move(RecoveryCaseStatus to, RecoveryCaseStatus requiredFrom, DateTimeOffset at)
    {
        EnsureNotTerminal();
        if (Status != requiredFrom)
        {
            throw new DomainException("recovery_transition_invalid", $"Cannot change recovery case from {Status} to {to}.");
        }

        var from = Status;
        Status = to;
        if (to == RecoveryCaseStatus.Completed)
        {
            TerminalAt = at;
        }

        Raise(new RecoveryCaseStatusChangedDomainEvent(Id, UserId, from, to, Reason, at));
    }

    private void EnsureNotTerminal()
    {
        if (Status is RecoveryCaseStatus.Completed or RecoveryCaseStatus.Cancelled)
        {
            throw new DomainException("recovery_case_terminal", "Recovery case is terminal.");
        }
    }
}
