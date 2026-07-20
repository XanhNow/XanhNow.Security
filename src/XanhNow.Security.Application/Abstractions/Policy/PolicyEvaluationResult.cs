namespace XanhNow.Security.Application.Abstractions.Policy;

public sealed record PolicyEvaluationResult(bool Allowed, Guid PolicyId, int PolicyVersion, string ReasonCode)
{
    public static PolicyEvaluationResult Allow(Guid policyId, int policyVersion, string reasonCode = "policy_allowed") => new(true, policyId, policyVersion, reasonCode);
    public static PolicyEvaluationResult Deny(Guid policyId, int policyVersion, string reasonCode) => new(false, policyId, policyVersion, reasonCode);
}
