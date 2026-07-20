using System.Security.Cryptography;
using System.Text;
using XanhNow.Security.Infrastructure.Integration.Options;

namespace XanhNow.Security.Infrastructure.Integration.Vault;

public interface IGrantTokenService
{
    ValueTask<string> SignAsync(string subject, string purpose, TimeSpan ttl, CancellationToken cancellationToken);
    ValueTask<bool> VerifyAsync(string token, string purpose, CancellationToken cancellationToken);
}

internal sealed class VaultBackedGrantTokenService : IGrantTokenService
{
    private readonly VaultIntegrationOptions _options;

    public VaultBackedGrantTokenService(SecurityIntegrationOptions options) => _options = options.Vault;

    public ValueTask<string> SignAsync(string subject, string purpose, TimeSpan ttl, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        if (ttl <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ttl), "Grant ttl must be positive.");
        }

        var expiresAt = DateTimeOffset.UtcNow.Add(ttl).ToUnixTimeSeconds();
        var body = $"{subject}.{purpose}.{expiresAt}.{_options.GrantSigningKeyPath}";
        var signature = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        return ValueTask.FromResult(Convert.ToBase64String(Encoding.UTF8.GetBytes($"{body}.{signature}")));
    }

    public ValueTask<bool> VerifyAsync(string token, string purpose, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(purpose))
        {
            return ValueTask.FromResult(false);
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = decoded.Split('.');
            if (parts.Length < 5 || !string.Equals(parts[1], purpose, StringComparison.Ordinal))
            {
                return ValueTask.FromResult(false);
            }

            return ValueTask.FromResult(long.TryParse(parts[2], out var expiresAt) && expiresAt > DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        }
        catch (FormatException)
        {
            return ValueTask.FromResult(false);
        }
    }
}
