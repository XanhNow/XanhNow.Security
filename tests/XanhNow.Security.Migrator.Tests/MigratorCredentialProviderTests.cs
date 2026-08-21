using Microsoft.Extensions.Options;
using Npgsql;
using XanhNow.Security.Infrastructure.Integration.Vault;
using XanhNow.Security.Migrator.Credentials;
using XanhNow.Security.Migrator.Options;
using OptionsFactory = Microsoft.Extensions.Options.Options;

namespace XanhNow.Security.Migrator.Tests;

public sealed class MigratorCredentialProviderTests
{
    [Fact]
    public async Task LoadConnectionStringAsync_ReadsEnvironmentAndDisablesPooling()
    {
        var variable = $"SECURITY_MIGRATOR_TEST_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(variable, "Host=localhost;Database=authtest;Username=s101_xanhnow_auth_security_migrator;Password=secret;Pooling=true;Include Error Detail=true");
        try
        {
            var provider = new MigratorCredentialProvider(OptionsFactory.Create(new MigratorOptions
            {
                Credential = new MigratorCredentialOptions { Provider = "Environment", EnvVarName = variable }
            }), Array.Empty<IVaultSecretReader>());

            var connectionString = await provider.LoadConnectionStringAsync(CancellationToken.None);
            var builder = new NpgsqlConnectionStringBuilder(connectionString);

            Assert.False(builder.Pooling);
            Assert.False(builder.IncludeErrorDetail);
        }
        finally
        {
            Environment.SetEnvironmentVariable(variable, null);
        }
    }

    [Fact]
    public async Task LoadConnectionStringAsync_ThrowsWhenCredentialMissing()
    {
        var provider = new MigratorCredentialProvider(OptionsFactory.Create(new MigratorOptions
        {
            Credential = new MigratorCredentialOptions { Provider = "Environment", EnvVarName = $"MISSING_{Guid.NewGuid():N}" }
        }), Array.Empty<IVaultSecretReader>());

        await Assert.ThrowsAsync<MigratorCredentialException>(() => provider.LoadConnectionStringAsync(CancellationToken.None));
    }
}
