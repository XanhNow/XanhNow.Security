using XanhNow.Security.Domain.Common;
using XanhNow.Security.Domain.Grants;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Domain.Tests.Grants;

public sealed class SecurityGrantTests
{
    [Fact]
    public void Grant_CanMoveIssuedActiveConsumed()
    {
        var issuedAt = DateTimeOffset.Parse("2026-07-18T01:07:00Z");
        var grant = SecurityGrant.Issue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SecurityGrantType.AuthGrant,
            GrantAudience.From("flutter-client"),
            GrantPurpose.From("login"),
            issuedAt,
            issuedAt.AddMinutes(5));

        grant.Activate(issuedAt.AddSeconds(10));
        grant.Consume(issuedAt.AddSeconds(20));

        Assert.Equal(SecurityGrantStatus.Consumed, grant.Status);
        Assert.NotNull(grant.TerminalAt);
    }

    [Fact]
    public void IssuedGrant_CannotBeConsumedDirectly()
    {
        var issuedAt = DateTimeOffset.Parse("2026-07-18T01:07:00Z");
        var grant = SecurityGrant.Issue(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SecurityGrantType.AuthGrant,
            GrantAudience.From("flutter-client"),
            GrantPurpose.From("login"),
            issuedAt,
            issuedAt.AddMinutes(5));

        var ex = Assert.Throws<DomainException>(() => grant.Consume(issuedAt.AddSeconds(20)));

        Assert.Equal("grant_transition_invalid", ex.Code);
    }
}


