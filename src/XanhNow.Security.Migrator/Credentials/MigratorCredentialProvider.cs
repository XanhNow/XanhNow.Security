using Microsoft.Extensions.Options;
using Npgsql;
using XanhNow.Security.Infrastructure.Integration.Vault;
using XanhNow.Security.Migrator.Options;

namespace XanhNow.Security.Migrator.Credentials;

public sealed class MigratorCredentialProvider : IMigratorCredentialProvider
{
    private readonly MigratorOptions _options;
    private readonly IVaultSecretReader? _vault;
    private string? _cachedConnectionString;

    public MigratorCredentialProvider(IOptions<MigratorOptions> options, IEnumerable<IVaultSecretReader> vaultReaders)
    {
        _options = options.Value;
        _vault = vaultReaders.FirstOrDefault();
    }

    public async Task<string> LoadConnectionStringAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_cachedConnectionString))
        {
            return _cachedConnectionString;
        }

        string? raw = null;
        if (string.Equals(_options.Credential.Provider, "Environment", StringComparison.OrdinalIgnoreCase))
        {
            raw = Environment.GetEnvironmentVariable(_options.Credential.EnvVarName);
        }
        else
        {
            if (_vault is null)
            {
                throw new MigratorCredentialException("Vault credential provider is not registered.");
            }

            raw = await _vault.ReadFieldAsync(new VaultSecretReference(_options.Credential.VaultSecretPath, _options.Credential.VaultField), cancellationToken).ConfigureAwait(false);
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new MigratorCredentialException("Security migrator PostgreSQL credential is unavailable.");
        }

        var builder = new NpgsqlConnectionStringBuilder(raw)
        {
            Pooling = false,
            IncludeErrorDetail = false
        };

        _cachedConnectionString = builder.ConnectionString;
        return _cachedConnectionString;
    }
}
