using XanhNow.Security.Domain.Common;
using XanhNow.Security.Domain.Policies;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Domain.Tests.Policies;

public sealed class SecurityPolicyTests
{
    [Fact]
    public void DraftPolicy_CanBeActivatedAndRetired()
    {
        var policy = SecurityPolicy.CreateDraft(Guid.NewGuid(), PolicyCode.From("login-risk"), 1, "{ \"allow\": true }", DateTimeOffset.Parse("2026-07-18T01:07:00Z"));

        policy.Activate(Guid.NewGuid(), DateTimeOffset.Parse("2026-07-18T01:08:00Z"));
        policy.Retire(Guid.NewGuid(), DateTimeOffset.Parse("2026-07-18T01:09:00Z"));

        Assert.Equal(SecurityPolicyStatus.Retired, policy.Status);
    }

    [Fact]
    public void DraftPolicy_CannotCreateNextDraft()
    {
        var policy = SecurityPolicy.CreateDraft(Guid.NewGuid(), PolicyCode.From("login-risk"), 1, "{ \"allow\": true }", DateTimeOffset.Parse("2026-07-18T01:07:00Z"));

        var ex = Assert.Throws<DomainException>(() => policy.CreateNextDraft(Guid.NewGuid(), "{ \"allow\": false }", DateTimeOffset.Parse("2026-07-18T01:08:00Z")));

        Assert.Equal("policy_next_version_requires_active", ex.Code);
    }
}
