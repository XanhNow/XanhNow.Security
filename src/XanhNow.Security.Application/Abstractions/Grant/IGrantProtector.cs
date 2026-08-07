namespace XanhNow.Security.Application.Abstractions.Grant;

public sealed record ProtectedGrant(string Value, DateTimeOffset ExpiresAt)
{
    public override string ToString() => "[PROTECTED_GRANT]";
}

public sealed record ProtectedGrantVerification(bool IsValid, Guid? GrantId, Guid? UserId, string? Purpose);

public interface IGrantProtector
{
    ValueTask<ProtectedGrant> ProtectAsync(Guid grantId, Guid userId, string purpose, DateTimeOffset expiresAt, CancellationToken cancellationToken);
    ValueTask<ProtectedGrantVerification> VerifyAsync(string protectedGrant, string expectedPurpose, CancellationToken cancellationToken);
}

public interface IReplayGuard
{
    ValueTask<bool> TryMarkUsedAsync(string replayKey, DateTimeOffset expiresAt, CancellationToken cancellationToken);
}
