using XanhNow.Security.Domain.Common;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Domain.Policies;

public sealed class SecurityPolicyDecision : Entity<Guid>
{
    private SecurityPolicyDecision()
    {
    }

    private SecurityPolicyDecision(Guid id, Guid userId, PolicyCode policyCode, int policyVersion, PolicyDecisionResult result, ReasonCode reason, DateTimeOffset decidedAt) : base(Guard.NotEmpty(id, nameof(id)))
    {
        Guard.NotEmpty(userId, nameof(userId));
        Guard.True(policyVersion > 0, "policy_version_invalid", "Policy version must be positive.");
        UserId = userId;
        PolicyCode = policyCode;
        PolicyVersion = policyVersion;
        Result = result;
        Reason = reason;
        DecidedAt = decidedAt;
    }

    public Guid UserId { get; private set; }
    public PolicyCode PolicyCode { get; private set; } = null!;
    public int PolicyVersion { get; private set; }
    public PolicyDecisionResult Result { get; private set; }
    public ReasonCode Reason { get; private set; } = null!;
    public DateTimeOffset DecidedAt { get; private set; }

    public static SecurityPolicyDecision Create(Guid id, Guid userId, PolicyCode policyCode, int policyVersion, PolicyDecisionResult result, ReasonCode reason, DateTimeOffset decidedAt)
        => new(id, userId, policyCode, policyVersion, result, reason, decidedAt);
}
