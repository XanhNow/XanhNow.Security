using XanhNow.Security.Domain.Common;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Domain.Users.Events;

public sealed record SecurityUserStatusChangedDomainEvent(
    Guid UserId,
    UserSecurityStatus From,
    UserSecurityStatus To,
    ReasonCode Reason,
    DateTimeOffset OccurredAt) : IDomainEvent;
