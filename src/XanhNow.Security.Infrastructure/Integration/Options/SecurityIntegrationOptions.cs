namespace XanhNow.Security.Infrastructure.Integration.Options;

public sealed class SecurityIntegrationOptions
{
    public ChildAppClientOptions AuthLogin { get; set; } = new() { Name = "Auth_Login_App", BaseAddress = "http://127.0.0.1:8080" };
    public ChildAppClientOptions Jwt { get; set; } = new() { Name = "JWT_Refresh_Token_App", BaseAddress = "http://127.0.0.1:5102" };
    public ChildAppClientOptions Passkey { get; set; } = new() { Name = "Passkey_Provider_App", BaseAddress = "http://127.0.0.1:5101" };
    public ChildAppClientOptions SmartOtp { get; set; } = new() { Name = "SmartOtp_App", BaseAddress = "https://127.0.0.1:5104", RequiresMtls = true };
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
    public string RoleIdFile { get; set; } = string.Empty;
    public string SecretIdFile { get; set; } = string.Empty;
    public string? CaCertificatePath { get; set; }
    public string GrantSigningKeyPath { get; set; } = "kv/xanhnow/security/grants/signing";
}

public sealed class RedisIntegrationOptions
{
    public string Configuration { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = "xanhnow:security";
    public TimeSpan DefaultCacheTtl { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan IdempotencyTtl { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan LockTtl { get; set; } = TimeSpan.FromSeconds(30);
}

public sealed class KafkaIntegrationOptions
{
    public string BootstrapServers { get; set; } = string.Empty;
    public string SecurityEventsTopic { get; set; } = "xanhnow.security.events";
    public string SecurityAuditTopic { get; set; } = "xanhnow.security.audit";
    public bool EnableIdempotentProducer { get; set; } = true;
}
