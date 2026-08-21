param(
    [string]$PsqlPath = "C:\Program Files\PostgreSQL\18\bin\psql.exe",
    [string]$HostName = "192.168.2.80",
    [int]$Port = 15432,
    [string]$Database = "authtest",
    [string]$AdminUser = "postgres",
    [string]$RootCert = "C:\BackEndXanhNow\XanhnowCustomer\secrets\postgresql-root-ca.crt"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $PsqlPath)) { throw "psql not found: $PsqlPath" }
if (-not (Test-Path -LiteralPath $RootCert)) { throw "PostgreSQL root cert not found: $RootCert" }

$migrator = "s101_xanhnow_auth_security_migrator"
$runtime = "s101_xanhnow_auth_security_runtime"

Write-Host "This script provisions XanhNow_Security_App PostgreSQL roles/schema in database $Database."
Write-Host "Roles: $migrator, $runtime"

function ConvertTo-PlainText([securestring]$Value) {
    $ptr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($Value)
    try { [Runtime.InteropServices.Marshal]::PtrToStringBSTR($ptr) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($ptr) }
}

$migratorPassword = ConvertTo-PlainText (Read-Host "New password for $migrator" -AsSecureString)
$runtimePassword = ConvertTo-PlainText (Read-Host "New password for $runtime" -AsSecureString)

$env:PGSSLMODE = "verify-full"
$env:PGSSLROOTCERT = $RootCert

$sql = @"
DO `$`$
DECLARE
    migrator_name text := '$migrator';
    runtime_name text := '$runtime';
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = migrator_name) THEN
        EXECUTE format('CREATE ROLE %I LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS', migrator_name, '$($migratorPassword.Replace("'","''"))');
    ELSE
        EXECUTE format('ALTER ROLE %I WITH LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS', migrator_name, '$($migratorPassword.Replace("'","''"))');
    END IF;

    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = runtime_name) THEN
        EXECUTE format('CREATE ROLE %I LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS', runtime_name, '$($runtimePassword.Replace("'","''"))');
    ELSE
        EXECUTE format('ALTER ROLE %I WITH LOGIN PASSWORD %L NOSUPERUSER NOCREATEDB NOCREATEROLE NOINHERIT NOREPLICATION NOBYPASSRLS', runtime_name, '$($runtimePassword.Replace("'","''"))');
    END IF;

    EXECUTE format('GRANT CONNECT ON DATABASE $Database TO %I', migrator_name);
    EXECUTE format('GRANT CONNECT ON DATABASE $Database TO %I', runtime_name);

    IF NOT EXISTS (SELECT 1 FROM information_schema.schemata WHERE schema_name = 'security') THEN
        EXECUTE format('CREATE SCHEMA security AUTHORIZATION %I', migrator_name);
    ELSE
        EXECUTE format('ALTER SCHEMA security OWNER TO %I', migrator_name);
    END IF;

    EXECUTE format('REVOKE CREATE ON DATABASE $Database FROM %I', migrator_name);
    EXECUTE format('REVOKE CREATE ON DATABASE $Database FROM %I', runtime_name);
    EXECUTE format('REVOKE CREATE ON SCHEMA security FROM %I', runtime_name);
END
`$`$;
"@

try {
    & $PsqlPath -h $HostName -p $Port -U $AdminUser -d $Database -v ON_ERROR_STOP=1 -c $sql
    if ($LASTEXITCODE -ne 0) { throw "psql provision failed with exit code $LASTEXITCODE" }

    $env:PGPASSWORD = $migratorPassword
    & $PsqlPath -h $HostName -p $Port -U $migrator -d $Database -w -c "select current_user, current_database();"
    if ($LASTEXITCODE -ne 0) { throw "migrator login verification failed with exit code $LASTEXITCODE" }

    $env:PGPASSWORD = $runtimePassword
    & $PsqlPath -h $HostName -p $Port -U $runtime -d $Database -w -c "select current_user, current_database();"
    if ($LASTEXITCODE -ne 0) { throw "runtime login verification failed with exit code $LASTEXITCODE" }

    Write-Host "XanhNow Security PostgreSQL roles/schema provisioned and verified."
}
finally {
    Remove-Item Env:\PGPASSWORD -ErrorAction SilentlyContinue
    Remove-Item Env:\PGSSLMODE -ErrorAction SilentlyContinue
    Remove-Item Env:\PGSSLROOTCERT -ErrorAction SilentlyContinue
    $migratorPassword = $null
    $runtimePassword = $null
}
