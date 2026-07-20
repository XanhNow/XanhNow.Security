using XanhNow.Security.Domain.Common;
using XanhNow.Security.Domain.Profiles;
using XanhNow.Security.Domain.Sessions;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Domain.Tests.Projections;

public sealed class ProjectionTests
{
    [Fact]
    public void SecurityProfile_RejectsStaleSnapshot()
    {
        var profile = SecurityProfile.Create(Guid.NewGuid(), 1, 1, passwordLoginEnabled: true, DateTimeOffset.Parse("2026-07-18T01:10:00Z"));

        var ex = Assert.Throws<DomainException>(() => profile.ApplySnapshot(1, 1, passwordLoginEnabled: true, DateTimeOffset.Parse("2026-07-18T01:09:00Z")));

        Assert.Equal("profile_snapshot_stale", ex.Code);
    }

    [Fact]
    public void SecuritySession_CanBeRevokedIdempotently()
    {
        var session = SecuritySession.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            JtiHash.From("sha256-jti-hash"),
            DateTimeOffset.Parse("2026-07-18T01:07:00Z"),
            DateTimeOffset.Parse("2026-07-18T02:07:00Z"));

        session.MarkRevoked(DateTimeOffset.Parse("2026-07-18T01:10:00Z"));
        session.MarkRevoked(DateTimeOffset.Parse("2026-07-18T01:11:00Z"));

        Assert.True(session.IsRevoked);
        Assert.Equal(DateTimeOffset.Parse("2026-07-18T01:10:00Z"), session.RevokedAt);
    }
}

