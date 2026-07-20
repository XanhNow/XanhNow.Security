namespace XanhNow.Security.Application.Abstractions.Grant;

public sealed record ProtectedGrant(string Value, DateTimeOffset ExpiresAt)
{
    public override string ToString() => "[PROTECTED_GRANT]";
}

public interface IGrantProtector
{
    ValueTask<ProtectedGrant> ProtectAsync(Guid grantId, Guid userId, string purpose, DateTimeOffset expiresAt, CancellationToken cancellationToken);
}

public interface IReplayGuard
{
    ValueTask<bool> TryMarkUsedAsync(string replayKey, DateTimeOffset expiresAt, CancellationToken cancellationToken);
}
