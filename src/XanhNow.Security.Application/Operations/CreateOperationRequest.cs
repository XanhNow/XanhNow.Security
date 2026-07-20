using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Application.Operations;

public sealed record CreateOperationRequest(Guid UserId, OperationTypeCode OperationType, IdempotencyKey IdempotencyKey, TimeSpan TimeToLive, IReadOnlyCollection<OperationStepPlan> Steps);
