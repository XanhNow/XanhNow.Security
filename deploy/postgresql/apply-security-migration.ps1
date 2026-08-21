param(
    [string]$ProjectRoot = "C:\BackEndXanhNow\XanhnowAuth\XanhNow_Security_App",
    [string]$PsqlPath = "C:\Program Files\PostgreSQL\18\bin\psql.exe",
    [string]$HostName = "192.168.2.80",
    [int]$Port = 15432,
    [string]$Database = "authtest",
    [string]$RootCert = "C:\BackEndXanhNow\XanhnowCustomer\secrets\postgresql-root-ca.crt"
)

$ErrorActionPreference = "Stop"

function Convert-SecureStringToPlainText([securestring]$Value) {
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try { [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
}

$migrator = "s101_xanhnow_auth_security_migrator"
$runtime = "s101_xanhnow_auth_security_runtime"
$migratorPassword = Convert-SecureStringToPlainText (Read-Host "Password $migrator" -AsSecureString)
$connectionString = "Host=$HostName;Port=$Port;Database=$Database;Username=$migrator;Password=$migratorPassword;SSL Mode=VerifyFull;Root Certificate=$RootCert;Pooling=false;Timeout=15;Command Timeout=60"

Push-Location $ProjectRoot
try {
    $env:DOTNET_ENVIRONMENT = "Production"
    $env:ASPNETCORE_ENVIRONMENT = "Production"
    $env:SECURITY_MIGRATOR_CONNECTION_STRING = $connectionString
    $env:SecurityMigrator__Credential__Provider = "Environment"
    $env:SecurityMigrator__AllowApply = "true"

    dotnet run --project src\XanhNow.Security.Migrator\XanhNow.Security.Migrator.csproj -- apply
    if ($LASTEXITCODE -ne 0) { throw "migration failed with exit code $LASTEXITCODE" }

    $grantSql = @"
GRANT USAGE ON SCHEMA security TO $runtime;
REVOKE CREATE ON SCHEMA security FROM $runtime;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA security TO $runtime;
REVOKE TRUNCATE, REFERENCES, TRIGGER ON ALL TABLES IN SCHEMA security FROM $runtime;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA security TO $runtime;
ALTER DEFAULT PRIVILEGES FOR ROLE $migrator IN SCHEMA security GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO $runtime;
ALTER DEFAULT PRIVILEGES FOR ROLE $migrator IN SCHEMA security GRANT USAGE, SELECT ON SEQUENCES TO $runtime;
DO `$`$
DECLARE
    row_count integer;
BEGIN
    SELECT count(*) INTO row_count
    FROM pg_tables
    WHERE schemaname = 'security'
      AND tableowner = '$runtime';
    IF row_count > 0 THEN
        RAISE EXCEPTION 'runtime role owns security tables';
    END IF;
END
`$`$;
"@

    $env:PGSSLMODE = "verify-full"
    $env:PGSSLROOTCERT = $RootCert
    $env:PGPASSWORD = $migratorPassword
    & $PsqlPath -h $HostName -p $Port -U $migrator -d $Database -v ON_ERROR_STOP=1 -c $grantSql
    if ($LASTEXITCODE -ne 0) { throw "runtime grant failed with exit code $LASTEXITCODE" }

    Write-Host "XanhNow Security migration applied and runtime privileges verified."
}
finally {
    Remove-Item Env:\SECURITY_MIGRATOR_CONNECTION_STRING -ErrorAction SilentlyContinue
    Remove-Item Env:\SecurityMigrator__Credential__Provider -ErrorAction SilentlyContinue
    Remove-Item Env:\SecurityMigrator__AllowApply -ErrorAction SilentlyContinue
    Remove-Item Env:\DOTNET_ENVIRONMENT -ErrorAction SilentlyContinue
    Remove-Item Env:\ASPNETCORE_ENVIRONMENT -ErrorAction SilentlyContinue
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:\PGSSLMODE -ErrorAction SilentlyContinue
    Remove-Item Env:\PGSSLROOTCERT -ErrorAction SilentlyContinue
    $migratorPassword = $null
    Pop-Location
}
