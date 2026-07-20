using XanhNow.Security.Domain.Common;
using XanhNow.Security.Domain.ValueObjects;

namespace XanhNow.Security.Domain.Sessions;

public sealed class SecuritySession : Entity<Guid>
{
    private SecuritySession()
    {
    }

    private SecuritySession(Guid sessionId, Guid userId, JtiHash jtiHash, DateTimeOffset issuedAt, DateTimeOffset expiresAt) : base(Guard.NotEmpty(sessionId, nameof(sessionId)))
    {
        Guard.NotEmpty(userId, nameof(userId));
        Guard.True(expiresAt > issuedAt, "session_expiry_invalid", "Session expiry must be after issued time.");
        UserId = userId;
        JtiHash = jtiHash;
        IssuedAt = issuedAt;
        ExpiresAt = expiresAt;
    }

    public Guid UserId { get; private set; }
    public JtiHash JtiHash { get; private set; } = null!;
    public DateTimeOffset IssuedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public bool IsRevoked => RevokedAt.HasValue;

    public static SecuritySession Create(Guid sessionId, Guid userId, JtiHash jtiHash, DateTimeOffset issuedAt, DateTimeOffset expiresAt)
        => new(sessionId, userId, jtiHash, issuedAt, expiresAt);

    public void MarkRevoked(DateTimeOffset revokedAt)
    {
        RevokedAt ??= revokedAt;
    }
}
