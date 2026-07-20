using XanhNow.Security.Domain.Common;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Domain.Grants.Events;

public sealed record SecurityGrantStatusChangedDomainEvent(
    Guid GrantId,
    Guid UserId,
    SecurityGrantStatus From,
    SecurityGrantStatus To,
    ReasonCode Reason,
    DateTimeOffset OccurredAt) : IDomainEvent;
