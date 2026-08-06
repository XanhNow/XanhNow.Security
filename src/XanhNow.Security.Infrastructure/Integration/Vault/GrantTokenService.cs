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
    private readonly IVaultSecretReader _secrets;

    public VaultBackedGrantTokenService(SecurityIntegrationOptions options, IVaultSecretReader secrets)
    {
        _options = options.Vault;
        _secrets = secrets;
    }

    public async ValueTask<string> SignAsync(string subject, string purpose, TimeSpan ttl, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(subject);
        ArgumentException.ThrowIfNullOrWhiteSpace(purpose);
        if (ttl <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(ttl), "Grant ttl must be positive.");
        }

        var expiresAt = DateTimeOffset.UtcNow.Add(ttl).ToUnixTimeSeconds();
        var body = $"{subject}.{purpose}.{expiresAt}";
        var signature = await SignBodyAsync(body, cancellationToken);
        return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{body}.{signature}"));
    }

    public async ValueTask<bool> VerifyAsync(string token, string purpose, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(purpose))
        {
            return false;
        }

        try
        {
            var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(token));
            var parts = decoded.Split('.');
            if (parts.Length != 4 || !string.Equals(parts[1], purpose, StringComparison.Ordinal))
            {
                return false;
            }

            if (!long.TryParse(parts[2], out var expiresAt) || expiresAt <= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                return false;
            }

            var body = $"{parts[0]}.{parts[1]}.{parts[2]}";
            var expected = await SignBodyAsync(body, cancellationToken);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(expected),
                Encoding.UTF8.GetBytes(parts[3]));
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private async ValueTask<string> SignBodyAsync(string body, CancellationToken cancellationToken)
    {
        var key = await _secrets.ReadFieldAsync(new VaultSecretReference(_options.GrantSigningKeyPath, _options.GrantSigningKeyField), cancellationToken);
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException($"Vault grant signing secret '{_options.GrantSigningKeyPath}' is missing field '{_options.GrantSigningKeyField}'.");
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
    }
}
