namespace XanhNow.Security.Infrastructure.Integration.Options;

public sealed class SecurityIntegrationOptions
{
    public ChildAppClientOptions AuthLogin { get; set; } = new() { Name = "Auth_Login_App", BaseAddress = "http://127.0.0.1:8080" };
    public ChildAppClientOptions Jwt { get; set; } = new() { Name = "JWT_Refresh_Token_App", BaseAddress = "http://127.0.0.1:5102" };
    public ChildAppClientOptions Passkey { get; set; } = new() { Name = "Passkey_Provider_App", BaseAddress = "http://127.0.0.1:5101" };
    public ChildAppClientOptions SmartOtp { get; set; } = new() { Name = "SmartOtp_App", BaseAddress = "http://127.0.0.1:5104" };
    public VaultIntegrationOptions Vault { get; set; } = new();
    public RedisIntegrationOptions Redis { get; set; } = new();
    public KafkaIntegrationOptions Kafka { get; set; } = new();
    public TimeSpan DefaultDeadline { get; set; } = TimeSpan.FromSeconds(8);
    public string ContractVersion { get; set; } = "v1";
}

public sealed class ChildAppClientOptions
{
    public string Name { get; set; } = string.Empty;
    public string BaseAddress { get; set; } = string.Empty;
    public TimeSpan Deadline { get; set; } = TimeSpan.FromSeconds(8);
    public bool RequiresMtls { get; set; }
    public string? ClientCertificatePath { get; set; }
    public string? ClientCertificateKeyPath { get; set; }
    public string? TrustedCaPath { get; set; }
}

public sealed class VaultIntegrationOptions
{
    public string Address { get; set; } = string.Empty;
    public string AuthMount { get; set; } = "approle";
    public string RoleIdEnvironmentVariable { get; set; } = "XANHNOW_SECURITY_VAULT_ROLE_ID";
    public string SecretIdEnvironmentVariable { get; set; } = "XANHNOW_SECURITY_VAULT_SECRET_ID";
    public string RoleIdFile { get; set; } = string.Empty;
    public string SecretIdFile { get; set; } = string.Empty;
    public string? CaCertificatePath { get; set; }
    public string? CaCertFile { get; set; }
    public string GrantSigningKeyPath { get; set; } = "kv/xanhnow/s101/security/grants/signing";
    public string GrantSigningKeyField { get; set; } = "signing_key";
    public string GrantSigningKeyFile { get; set; } = string.Empty;
    public string PostgresApiSecretPath { get; set; } = "kv/xanhnow/s101/security/postgres/runtime";
    public string PostgresConnectionStringField { get; set; } = "connection_string";
    public string PostgresConnectionStringFile { get; set; } = string.Empty;
}

public sealed class RedisIntegrationOptions
{
    public string Mode { get; set; } = "InMemory";
    public string Configuration { get; set; } = string.Empty;
    public string ConfigurationFile { get; set; } = string.Empty;
    public string BootstrapEndpoints { get; set; } = string.Empty;
    public string SecretPath { get; set; } = string.Empty;
    public string PasswordField { get; set; } = "password";
    public string PasswordFile { get; set; } = string.Empty;
    public bool TlsEnabled { get; set; }
    public string KeyPrefix { get; set; } = "s101:security";
    public string KeyPrefixFile { get; set; } = string.Empty;
    public TimeSpan DefaultCacheTtl { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan IdempotencyTtl { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan LockTtl { get; set; } = TimeSpan.FromSeconds(30);
    public int ConnectTimeoutMs { get; set; } = 5000;
    public int OperationTimeoutMs { get; set; } = 3000;
    public bool AbortOnConnectFail { get; set; }
}

public sealed class KafkaIntegrationOptions
{
    public string Mode { get; set; } = "InMemory";
    public string BootstrapServers { get; set; } = string.Empty;
    public string BootstrapServersFile { get; set; } = string.Empty;
    public string SecurityEventsTopic { get; set; } = "xanhnow.security.events";
    public string SecurityAuditTopic { get; set; } = "xanhnow.security.audit";
    public string ClientId { get; set; } = "xanhnow-security-producer";
    public string Acks { get; set; } = "all";
    public bool EnableIdempotentProducer { get; set; } = true;
    public string SecretPath { get; set; } = string.Empty;
    public string UsernameField { get; set; } = "username";
    public string PasswordField { get; set; } = "password";
    public string SecurityProtocolField { get; set; } = "security_protocol";
    public string SaslMechanismField { get; set; } = "sasl_mechanism";
    public string SslCaLocation { get; set; } = string.Empty;
    public string UsernameFile { get; set; } = string.Empty;
    public string PasswordFile { get; set; } = string.Empty;
    public string SecurityProtocolFile { get; set; } = string.Empty;
    public string SaslMechanismFile { get; set; } = string.Empty;
    public string SslCaLocationFile { get; set; } = string.Empty;
}
