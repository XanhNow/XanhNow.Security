using XanhNow.Security.Application.Abstractions.Grant;

namespace XanhNow.Security.Infrastructure.Integration.Vault;

internal sealed class GrantProtector : IGrantProtector
{
    private readonly IGrantTokenService _tokens;

    public GrantProtector(IGrantTokenService tokens) => _tokens = tokens;

    public async ValueTask<ProtectedGrant> ProtectAsync(Guid grantId, Guid userId, string purpose, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        var ttl = expiresAt - DateTimeOffset.UtcNow;
        if (ttl <= TimeSpan.Zero)
        {
            ttl = TimeSpan.FromSeconds(1);
        }

        var subject = $"{userId:N}:{grantId:N}";
        var value = await _tokens.SignAsync(subject, purpose, ttl, cancellationToken);
        return new ProtectedGrant(value, expiresAt);
    }

    public async ValueTask<ProtectedGrantVerification> VerifyAsync(string protectedGrant, string expectedPurpose, CancellationToken cancellationToken)
    {
        var verified = await _tokens.VerifyAsync(protectedGrant, expectedPurpose, cancellationToken);
        if (!verified.IsValid || string.IsNullOrWhiteSpace(verified.Subject))
        {
            return new(false, null, null, null);
        }

        var subjectParts = verified.Subject.Split(':', 2);
        if (subjectParts.Length != 2 || !Guid.TryParseExact(subjectParts[0], "N", out var userId) || !Guid.TryParseExact(subjectParts[1], "N", out var grantId))
        {
            return new(false, null, null, null);
        }

        return new(true, grantId, userId, verified.Purpose);
    }
}
