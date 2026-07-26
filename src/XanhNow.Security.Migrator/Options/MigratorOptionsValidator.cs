using Microsoft.Extensions.Options;

namespace XanhNow.Security.Migrator.Options;

public sealed class MigratorOptionsValidator : IValidateOptions<MigratorOptions>
{
    private static readonly HashSet<string> SupportedProviders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Vault",
        "Environment"
    };

    public ValidateOptionsResult Validate(string? name, MigratorOptions options)
    {
        var failures = new List<string>();

        Require(options.EnvironmentName, nameof(options.EnvironmentName), failures);
        Require(options.ExpectedDatabase, nameof(options.ExpectedDatabase), failures);
        Require(options.ExpectedRole, nameof(options.ExpectedRole), failures);
        Require(options.LockKey, nameof(options.LockKey), failures);

        if (!string.Equals(options.ExpectedSchema, "security", StringComparison.Ordinal))
        {
            failures.Add("ExpectedSchema must be 'security'.");
        }

        if (!SupportedProviders.Contains(options.Credential.Provider))
        {
            failures.Add("Credential.Provider must be Vault or Environment.");
        }

        if (string.Equals(options.Credential.Provider, "Environment", StringComparison.OrdinalIgnoreCase))
        {
            Require(options.Credential.EnvVarName, "Credential.EnvVarName", failures);
        }

        if (string.Equals(options.Credential.Provider, "Vault", StringComparison.OrdinalIgnoreCase))
        {
            Require(options.Credential.VaultSecretPath, "Credential.VaultSecretPath", failures);
            Require(options.Credential.VaultField, "Credential.VaultField", failures);
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void Require(string? value, string name, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            failures.Add($"{name} is required.");
        }
    }
}
