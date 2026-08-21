param(
    [string]$VaultPath = "C:\Program Files\HashiCorp\Vault\vault.exe",
    [string]$VaultAddress = "https://192.168.2.81:8200",
    [string]$VaultCaCert = "C:\BackEndXanhNow\XanhnowAuth\XanhNow_Security_App\runtime\trust\vault-ca.crt",
    [string]$OutputDirectory = ""
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $VaultPath)) { throw "vault.exe not found: $VaultPath" }
if (-not (Test-Path -LiteralPath $VaultCaCert)) { throw "Vault CA cert not found: $VaultCaCert" }

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$runtimePolicy = "s101-xanhnow-auth-security-runtime-prod"
$migratorPolicy = "s101-xanhnow-auth-security-migrator-prod"
$runtimeRole = "s101-xanhnow-auth-security-runtime-prod"
$migratorRole = "s101-xanhnow-auth-security-migrator-prod"

$env:VAULT_ADDR = $VaultAddress
$env:VAULT_CACERT = $VaultCaCert

& $VaultPath policy write $runtimePolicy (Join-Path $repoRoot "deploy\vault\$runtimePolicy.hcl")
if ($LASTEXITCODE -ne 0) { throw "vault policy write failed for $runtimePolicy" }

& $VaultPath policy write $migratorPolicy (Join-Path $repoRoot "deploy\vault\$migratorPolicy.hcl")
if ($LASTEXITCODE -ne 0) { throw "vault policy write failed for $migratorPolicy" }

& $VaultPath write "auth/approle/role/$runtimeRole" `
    "token_policies=$runtimePolicy" `
    "token_ttl=1h" `
    "token_max_ttl=24h" `
    "secret_id_ttl=720h" `
    "secret_id_num_uses=0"
if ($LASTEXITCODE -ne 0) { throw "vault approle write failed for $runtimeRole" }

& $VaultPath write "auth/approle/role/$migratorRole" `
    "token_policies=$migratorPolicy" `
    "token_ttl=30m" `
    "token_max_ttl=2h" `
    "secret_id_ttl=24h" `
    "secret_id_num_uses=1"
if ($LASTEXITCODE -ne 0) { throw "vault approle write failed for $migratorRole" }

$runtimeRoleId = (& $VaultPath read -field=role_id "auth/approle/role/$runtimeRole/role-id").Trim()
if ($LASTEXITCODE -ne 0) { throw "vault role-id read failed for $runtimeRole" }

$runtimeSecretId = (& $VaultPath write -field=secret_id -f "auth/approle/role/$runtimeRole/secret-id").Trim()
if ($LASTEXITCODE -ne 0) { throw "vault secret-id write failed for $runtimeRole" }

$migratorRoleId = (& $VaultPath read -field=role_id "auth/approle/role/$migratorRole/role-id").Trim()
if ($LASTEXITCODE -ne 0) { throw "vault role-id read failed for $migratorRole" }

$migratorSecretId = (& $VaultPath write -field=secret_id -f "auth/approle/role/$migratorRole/secret-id").Trim()
if ($LASTEXITCODE -ne 0) { throw "vault secret-id write failed for $migratorRole" }

if (-not [string]::IsNullOrWhiteSpace($OutputDirectory)) {
    New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
    $runtimeRoleId | Out-File -FilePath (Join-Path $OutputDirectory "runtime-role_id") -Encoding ascii -NoNewline
    $runtimeSecretId | Out-File -FilePath (Join-Path $OutputDirectory "runtime-secret_id") -Encoding ascii -NoNewline
    $migratorRoleId | Out-File -FilePath (Join-Path $OutputDirectory "migrator-role_id") -Encoding ascii -NoNewline
    $migratorSecretId | Out-File -FilePath (Join-Path $OutputDirectory "migrator-secret_id") -Encoding ascii -NoNewline
    Write-Host "AppRole material written to $OutputDirectory. Do not commit these files."
}

Write-Host "runtime_role_id_length=$($runtimeRoleId.Length)"
Write-Host "runtime_secret_id_length=$($runtimeSecretId.Length)"
Write-Host "migrator_role_id_length=$($migratorRoleId.Length)"
Write-Host "migrator_secret_id_length=$($migratorSecretId.Length)"
