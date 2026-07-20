using XanhNow.Security.Domain.Common;
using XanhNow.Security.Domain.Users;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Domain.Tests.Users;

public sealed class SecurityUserTests
{
    [Fact]
    public void Lock_ChangesActiveUserToLocked()
    {
        var user = SecurityUser.Create(Guid.NewGuid(), DateTimeOffset.Parse("2026-07-18T01:07:00Z"));

        user.Lock(ReasonCode.From("policy_violation"), DateTimeOffset.Parse("2026-07-18T01:08:00Z"));

        Assert.Equal(UserSecurityStatus.Locked, user.Status);
        Assert.Single(user.DomainEvents);
    }

    [Fact]
    public void DisabledUser_IsTerminal()
    {
        var user = SecurityUser.Create(Guid.NewGuid(), DateTimeOffset.Parse("2026-07-18T01:07:00Z"));
        user.Disable(ReasonCode.From("admin_disable"), DateTimeOffset.Parse("2026-07-18T01:08:00Z"));

        var ex = Assert.Throws<DomainException>(() => user.Lock(ReasonCode.From("late_lock"), DateTimeOffset.Parse("2026-07-18T01:09:00Z")));

        Assert.Equal("security_user_disabled_terminal", ex.Code);
    }
}

