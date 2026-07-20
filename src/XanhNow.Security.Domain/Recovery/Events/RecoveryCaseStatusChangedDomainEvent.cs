using XanhNow.Security.Domain.Common;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Domain.Recovery.Events;

public sealed record RecoveryCaseStatusChangedDomainEvent(
    Guid RecoveryCaseId,
    Guid UserId,
    RecoveryCaseStatus From,
    RecoveryCaseStatus To,
    ReasonCode Reason,
    DateTimeOffset OccurredAt) : IDomainEvent;
