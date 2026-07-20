using XanhNow.Security.Domain.Common;
using XanhNow.Security.Domain.Policies.Events;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Domain.Policies;

public sealed class SecurityPolicy : AggregateRoot<Guid>
{
    private SecurityPolicy()
    {
    }

    private SecurityPolicy(Guid id, PolicyCode code, int version, string rulesJson, DateTimeOffset createdAt) : base(Guard.NotEmpty(id, nameof(id)))
    {
        Guard.True(version > 0, "policy_version_invalid", "Policy version must be positive.");
        Code = code;
        Version = version;
        RulesJson = Guard.NotBlank(rulesJson, nameof(rulesJson), 16_000);
        Status = SecurityPolicyStatus.Draft;
        CreatedAt = createdAt;
    }

    public PolicyCode Code { get; private set; } = null!;
    public int Version { get; private set; }
    public string RulesJson { get; private set; } = string.Empty;
    public SecurityPolicyStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ActivatedAt { get; private set; }
    public Guid? ApprovedBy { get; private set; }

    public static SecurityPolicy CreateDraft(Guid id, PolicyCode code, int version, string rulesJson, DateTimeOffset createdAt)
        => new(id, code, version, rulesJson, createdAt);

    public SecurityPolicy CreateNextDraft(Guid newId, string rulesJson, DateTimeOffset createdAt)
    {
        Guard.True(Status == SecurityPolicyStatus.Active, "policy_next_version_requires_active", "Only active policy can create next draft.");
        return new SecurityPolicy(newId, Code, Version + 1, rulesJson, createdAt);
    }

    public void Activate(Guid approvedBy, DateTimeOffset occurredAt)
    {
        Guard.NotEmpty(approvedBy, nameof(approvedBy));
        if (Status != SecurityPolicyStatus.Draft)
        {
            throw new DomainException("policy_activate_invalid", "Only draft policy can be activated.");
        }

        var from = Status;
        Status = SecurityPolicyStatus.Active;
        ActivatedAt = occurredAt;
        ApprovedBy = approvedBy;
        Raise(new SecurityPolicyStatusChangedDomainEvent(Id, Code, Version, from, Status, approvedBy, occurredAt));
    }

    public void Retire(Guid approvedBy, DateTimeOffset occurredAt)
    {
        Guard.NotEmpty(approvedBy, nameof(approvedBy));
        if (Status != SecurityPolicyStatus.Active)
        {
            throw new DomainException("policy_retire_invalid", "Only active policy can be retired.");
        }

        var from = Status;
        Status = SecurityPolicyStatus.Retired;
        Raise(new SecurityPolicyStatusChangedDomainEvent(Id, Code, Version, from, Status, approvedBy, occurredAt));
    }
}
