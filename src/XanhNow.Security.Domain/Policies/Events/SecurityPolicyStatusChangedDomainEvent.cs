using XanhNow.Security.Domain.Common;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Domain.Policies.Events;

public sealed record SecurityPolicyStatusChangedDomainEvent(
    Guid PolicyId,
    PolicyCode Code,
    int Version,
    SecurityPolicyStatus From,
    SecurityPolicyStatus To,
    Guid ApprovedBy,
    DateTimeOffset OccurredAt) : IDomainEvent;
