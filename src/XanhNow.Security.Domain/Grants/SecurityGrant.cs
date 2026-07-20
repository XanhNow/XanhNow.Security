using XanhNow.Security.Domain.Common;
using XanhNow.Security.Domain.Grants.Events;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Domain.Grants;

public sealed class SecurityGrant : AggregateRoot<Guid>
{
    private SecurityGrant()
    {
    }

    private SecurityGrant(Guid id, Guid userId, SecurityGrantType type, GrantAudience audience, GrantPurpose purpose, DateTimeOffset issuedAt, DateTimeOffset expiresAt) : base(Guard.NotEmpty(id, nameof(id)))
    {
        Guard.NotEmpty(userId, nameof(userId));
        Guard.True(expiresAt > issuedAt, "grant_expiry_invalid", "Grant expiry must be after issued time.");
        UserId = userId;
        Type = type;
        Audience = audience;
        Purpose = purpose;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
        Status = SecurityGrantStatus.Issued;
    }

    public Guid UserId { get; private set; }
    public SecurityGrantType Type { get; private set; }
    public SecurityGrantStatus Status { get; private set; }
    public GrantAudience Audience { get; private set; } = null!;
    public GrantPurpose Purpose { get; private set; } = null!;
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? TerminalAt { get; private set; }

    public static SecurityGrant Issue(Guid id, Guid userId, SecurityGrantType type, GrantAudience audience, GrantPurpose purpose, DateTimeOffset issuedAt, DateTimeOffset expiresAt)
        => new(id, userId, type, audience, purpose, issuedAt, expiresAt);

    public void Activate(DateTimeOffset occurredAt) => Transition(SecurityGrantStatus.Active, ReasonCode.From("grant_activated"), occurredAt, SecurityGrantStatus.Issued);

    public void Consume(DateTimeOffset occurredAt) => Transition(SecurityGrantStatus.Consumed, ReasonCode.From("grant_consumed"), occurredAt, SecurityGrantStatus.Active);

    public void Expire(DateTimeOffset occurredAt) => Transition(SecurityGrantStatus.Expired, ReasonCode.From("grant_expired"), occurredAt, SecurityGrantStatus.Active);

    public void Revoke(ReasonCode reason, DateTimeOffset occurredAt) => Transition(SecurityGrantStatus.Revoked, reason, occurredAt, SecurityGrantStatus.Active);

    private void Transition(SecurityGrantStatus to, ReasonCode reason, DateTimeOffset occurredAt, SecurityGrantStatus requiredFrom)
    {
        if (Status != requiredFrom)
        {
            throw new DomainException("grant_transition_invalid", $"Cannot change grant from {Status} to {to}.");
        }

        var from = Status;
        Status = to;
        if (to is SecurityGrantStatus.Consumed or SecurityGrantStatus.Expired or SecurityGrantStatus.Revoked)
        {
            TerminalAt = occurredAt;
        }

        Raise(new SecurityGrantStatusChangedDomainEvent(Id, UserId, from, to, reason, occurredAt));
    }
}
