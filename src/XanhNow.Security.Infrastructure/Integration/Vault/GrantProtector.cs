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
}
