using XanhNow.Security.Domain.Audit;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Domain.Tests.Audit;

public sealed class SecurityAuditLogTests
{
    [Fact]
    public void AuditLog_IsCreatedAsAppendOnlyRecord()
    {
        var audit = SecurityAuditLog.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            AuditAction.From("login.accepted"),
            "accepted",
            ReasonCode.From("policy_passed"),
            TraceId.From("trace-001"),
            DateTimeOffset.Parse("2026-07-18T01:07:00Z"));

        Assert.Equal("login.accepted", audit.Action.Value);
        Assert.Equal("accepted", audit.Outcome);
    }
}
