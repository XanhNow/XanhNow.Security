namespace XanhNow.Security.Migrator.Options;

public sealed class MigratorOptions
{
    public const string SectionName = "SecurityMigrator";

    public string EnvironmentName { get; init; } = "Production";
    public string ExpectedSchema { get; init; } = "security";
    public string ExpectedDatabase { get; init; } = "authtest";
    public string ExpectedRole { get; init; } = "s101_xanhnow_auth_security_migrator";
    public string LockKey { get; init; } = "xanhnow.security.migrator";
    public bool AllowApply { get; init; }
    public MigratorCredentialOptions Credential { get; init; } = new();
}

public sealed class MigratorCredentialOptions
{
    public string Provider { get; init; } = "Vault";
    public string EnvVarName { get; init; } = "SECURITY_MIGRATOR_CONNECTION_STRING";
    public string VaultSecretPath { get; init; } = "kv/xanhnow/s101/security/postgres/migration";
    public string VaultField { get; init; } = "connection_string";
}
