param(
    [string]$VaultPath = "C:\Program Files\HashiCorp\Vault\vault.exe",
    [string]$VaultAddress = "https://192.168.2.81:8200",
    [string]$VaultCaCert = "C:\BackEndXanhNow\XanhnowAuth\XanhNow_Security_App\runtime\trust\vault-ca.crt",
    [string]$PostgresHost = "192.168.2.80",
    [int]$RuntimePort = 5432,
    [int]$MigrationPort = 15432,
    [string]$Database = "authtest",
    [string]$RuntimeUser = "s101_xanhnow_auth_security_runtime",
    [string]$MigratorUser = "s101_xanhnow_auth_security_migrator",
    [string]$PostgresRootCert = "C:\BackEndXanhNow\XanhnowCustomer\secrets\postgresql-root-ca.crt"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $VaultPath)) { throw "vault.exe not found: $VaultPath" }
if (-not (Test-Path -LiteralPath $VaultCaCert)) { throw "Vault CA cert not found: $VaultCaCert" }
if (-not (Test-Path -LiteralPath $PostgresRootCert)) { throw "PostgreSQL root cert not found: $PostgresRootCert" }

function ConvertTo-PlainText([securestring]$Value) {
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try { [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
}

function Read-OrCreateSigningKey {
    $existing = ""
    try {
        $value = & $VaultPath kv get -field=signing_key kv/xanhnow/s101/security/grants/signing 2>$null
        if ($LASTEXITCODE -eq 0) { $existing = (($value | Out-String).Trim()) }
    } catch { $existing = "" }

    if (-not [string]::IsNullOrWhiteSpace($existing)) {
        Write-Host "security_grant_signing_key reused from existing Vault value."
        return $existing
    }

    $bytes = New-Object byte[] 32
    $rng = [Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $rng.GetBytes($bytes)
    }
    finally {
        $rng.Dispose()
    }
    return [Convert]::ToBase64String($bytes)
}

$env:VAULT_ADDR = $VaultAddress
$env:VAULT_CACERT = $VaultCaCert

$runtimePassword = ConvertTo-PlainText (Read-Host "Password for PostgreSQL user $RuntimeUser" -AsSecureString)
$migratorPassword = ConvertTo-PlainText (Read-Host "Password for PostgreSQL user $MigratorUser" -AsSecureString)
$redisEndpoints = Read-Host "Redis endpoints for Security, for example 192.168.2.16:6379,192.168.2.33:6379,192.168.2.53:6379"
$redisPassword = ConvertTo-PlainText (Read-Host "Redis password for Security" -AsSecureString)
$kafkaBootstrapServers = Read-Host "Kafka bootstrap servers for Security, for example 192.168.2.14:9092,192.168.2.31:9092,192.168.2.51:9092"
$kafkaSecurityProtocol = Read-Host "Kafka security_protocol for Security, use PLAINTEXT if no SASL"
$kafkaSaslMechanism = Read-Host "Kafka sasl_mechanism for Security, use n/a if no SASL"
$kafkaUsername = Read-Host "Kafka username for Security, use n/a if no SASL"
$kafkaPassword = ConvertTo-PlainText (Read-Host "Kafka password for Security, enter n/a if no SASL" -AsSecureString)
$grantSigningKey = Read-OrCreateSigningKey

$escapedPostgresRootCert = $PostgresRootCert.Replace("\", "\\")
$runtimeConnectionString = "Host=$PostgresHost;Port=$RuntimePort;Database=$Database;Username=$RuntimeUser;Password=$runtimePassword;Pooling=true;No Reset On Close=true;Timeout=15;Command Timeout=30"
$migrationConnectionString = "Host=$PostgresHost;Port=$MigrationPort;Database=$Database;Username=$MigratorUser;Password=$migratorPassword;SSL Mode=VerifyFull;Root Certificate=$escapedPostgresRootCert;Pooling=false;Timeout=15;Command Timeout=60"
$redisConfiguration = "$redisEndpoints,password=$redisPassword,abortConnect=false,connectTimeout=5000,syncTimeout=3000,asyncTimeout=3000"
$redisKeyPrefix = "s101:security"

try {
    & $VaultPath kv put kv/xanhnow/s101/security/postgres/runtime "connection_string=$runtimeConnectionString"
    if ($LASTEXITCODE -ne 0) { throw "Vault postgres runtime secret write failed." }

    & $VaultPath kv put kv/xanhnow/s101/security/postgres/migration "connection_string=$migrationConnectionString"
    if ($LASTEXITCODE -ne 0) { throw "Vault postgres migration secret write failed." }

    & $VaultPath kv put kv/xanhnow/s101/security/redis `
        "configuration=$redisConfiguration" `
        "endpoints=$redisEndpoints" `
        "password=$redisPassword" `
        "key_prefix=$redisKeyPrefix"
    if ($LASTEXITCODE -ne 0) { throw "Vault redis secret write failed." }

    & $VaultPath kv put kv/xanhnow/s101/security/kafka `
        "bootstrap_servers=$kafkaBootstrapServers" `
        "security_protocol=$kafkaSecurityProtocol" `
        "sasl_mechanism=$kafkaSaslMechanism" `
        "username=$kafkaUsername" `
        "password=$kafkaPassword"
    if ($LASTEXITCODE -ne 0) { throw "Vault kafka secret write failed." }

    & $VaultPath kv put kv/xanhnow/s101/security/grants/signing "signing_key=$grantSigningKey"
    if ($LASTEXITCODE -ne 0) { throw "Vault grant signing secret write failed." }

    Write-Host "security_runtime_connection_string_length=$($runtimeConnectionString.Length)"
    Write-Host "security_migration_connection_string_length=$($migrationConnectionString.Length)"
    Write-Host "security_redis_configuration_length=$($redisConfiguration.Length)"
    Write-Host "security_redis_key_prefix_length=$($redisKeyPrefix.Length)"
    Write-Host "security_kafka_bootstrap_servers_length=$($kafkaBootstrapServers.Length)"
    Write-Host "security_grant_signing_key_length=$($grantSigningKey.Length)"
    Write-Host "XanhNow Security runtime and migration secrets written to Vault s101 paths."
}
finally {
    $runtimePassword = $null
    $migratorPassword = $null
    $redisPassword = $null
    $kafkaPassword = $null
    $grantSigningKey = $null
}
