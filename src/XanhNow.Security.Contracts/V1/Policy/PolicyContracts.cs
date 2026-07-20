using XanhNow.Security.Contracts.Common.Enums;

namespace XanhNow.Security.Contracts.V1.Policy;

public sealed record EvaluatePolicyRequest(string PolicyCode, Guid? UserId, string Purpose, IReadOnlyDictionary<string, string> Context);
public sealed record PolicyDecisionResponse(string PolicyCode, PolicyDecisionContract Decision, string ReasonCode, string PolicyVersion, DateTimeOffset EvaluatedAtUtc);
public sealed record PolicyDecisionSummaryResponse(Guid DecisionId, string PolicyCode, PolicyDecisionContract Decision, string ReasonCode, DateTimeOffset EvaluatedAtUtc);
