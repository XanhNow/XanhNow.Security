using XanhNow.Security.Domain.Common;
using XanhNow.Security.Domain.Recovery;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Domain.Tests.Recovery;

public sealed class SecurityRecoveryCaseTests
{
    [Fact]
    public void RecoveryCase_FollowsRequiredLifecycle()
    {
        var at = DateTimeOffset.Parse("2026-07-18T01:07:00Z");
        var recovery = SecurityRecoveryCase.Open(Guid.NewGuid(), Guid.NewGuid(), ReasonCode.From("lost_phone"), at);

        recovery.BeginProofVerification(at.AddMinutes(1));
        recovery.ProtectAccount(at.AddMinutes(2));
        recovery.RevokeSessions(at.AddMinutes(3));
        recovery.DisableAuthenticators(at.AddMinutes(4));
        recovery.RestoreAccess(at.AddMinutes(5));
        recovery.Complete(at.AddMinutes(6));

        Assert.Equal(RecoveryCaseStatus.Completed, recovery.Status);
        Assert.NotNull(recovery.TerminalAt);
    }

    [Fact]
    public void RecoveryCase_RejectsSkippedStep()
    {
        var recovery = SecurityRecoveryCase.Open(Guid.NewGuid(), Guid.NewGuid(), ReasonCode.From("lost_phone"), DateTimeOffset.Parse("2026-07-18T01:07:00Z"));

        var ex = Assert.Throws<DomainException>(() => recovery.RevokeSessions(DateTimeOffset.Parse("2026-07-18T01:08:00Z")));

        Assert.Equal("recovery_transition_invalid", ex.Code);
    }
}
