using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Application.Operations;

public sealed record OperationStepPlan(OperationTypeCode StepCode, bool Required);
