using XanhNow.Security.Domain.Common;
using XanhNow.Security.Domain.Users.Events;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Domain.Users;

public sealed class SecurityUser : AggregateRoot<Guid>
{
    private SecurityUser()
    {
    }

    private SecurityUser(Guid userId, DateTimeOffset createdAt) : base(Guard.NotEmpty(userId, nameof(userId)))
    {
        Status = UserSecurityStatus.Active;
        RiskLevel = RiskLevel.Low;
        CreatedAt = createdAt;
        UpdatedAt = createdAt;
    }

    public UserSecurityStatus Status { get; private set; }
    public RiskLevel RiskLevel { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public ReasonCode? LastReason { get; private set; }

    public static SecurityUser Create(Guid userId, DateTimeOffset createdAt) => new(userId, createdAt);

    public void Lock(ReasonCode reason, DateTimeOffset occurredAt) => ChangeStatus(UserSecurityStatus.Locked, reason, occurredAt);

    public void RequireRecovery(ReasonCode reason, DateTimeOffset occurredAt) => ChangeStatus(UserSecurityStatus.RecoveryRequired, reason, occurredAt);

    public void MarkCompromised(ReasonCode reason, DateTimeOffset occurredAt) => ChangeStatus(UserSecurityStatus.Compromised, reason, occurredAt);

    public void Disable(ReasonCode reason, DateTimeOffset occurredAt) => ChangeStatus(UserSecurityStatus.Disabled, reason, occurredAt);

    public void SetRisk(RiskLevel riskLevel, ReasonCode reason, DateTimeOffset occurredAt)
    {
        EnsureNotDisabled();
        RiskLevel = riskLevel;
        LastReason = reason;
        UpdatedAt = occurredAt;
    }

    private void ChangeStatus(UserSecurityStatus target, ReasonCode reason, DateTimeOffset occurredAt)
    {
        EnsureNotDisabled();
        if (Status == target)
        {
            return;
        }

        var previous = Status;
        Status = target;
        LastReason = reason;
        UpdatedAt = occurredAt;
        Raise(new SecurityUserStatusChangedDomainEvent(Id, previous, target, reason, occurredAt));
    }

    private void EnsureNotDisabled()
    {
        if (Status == UserSecurityStatus.Disabled)
        {
            throw new DomainException("security_user_disabled_terminal", "Disabled security user is terminal.");
        }
    }
}
