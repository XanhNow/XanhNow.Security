using XanhNow.Security.Domain.Common;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Domain.Operations.Events;

public sealed record OperationStatusChangedDomainEvent(
    Guid OperationId,
    Guid UserId,
    OperationTypeCode OperationType,
    SecurityOperationStatus FromStatus,
    SecurityOperationStatus ToStatus,
    DateTimeOffset OccurredAt) : IDomainEvent;
