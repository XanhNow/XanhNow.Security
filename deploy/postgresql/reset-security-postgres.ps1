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

Write-Host "This script deletes XanhNow_Security_App PostgreSQL schema/data and old/new roles in database $Database."
Write-Host "Schema: security"
Write-Host "Roles: s101_xanhnow_auth_security_migrator, s101_xanhnow_auth_security_runtime"
Write-Host "Legacy roles: xanhnow_security_migrator, xanhnow_security_api, xanhnow_security_worker, xanhnow_security"

$env:PGSSLMODE = "verify-full"
$env:PGSSLROOTCERT = $RootCert

$sql = @'
ALTER DATABASE authtest OWNER TO postgres;

DROP SCHEMA IF EXISTS security CASCADE;

DO $$
DECLARE
    role_name text;
    role_names text[] := ARRAY[
        's101_xanhnow_auth_security_migrator',
        's101_xanhnow_auth_security_runtime',
        'xanhnow_security_migrator',
        'xanhnow_security_api',
        'xanhnow_security_worker',
        'xanhnow_security'
    ];
BEGIN
    FOREACH role_name IN ARRAY role_names
    LOOP
        IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname = role_name) THEN
            EXECUTE format('REASSIGN OWNED BY %I TO postgres', role_name);
            EXECUTE format('DROP OWNED BY %I', role_name);
            EXECUTE format('DROP ROLE %I', role_name);
        END IF;
    END LOOP;
END
$$;
'@

try {
    & $PsqlPath -h $HostName -p $Port -U $AdminUser -d $Database -v ON_ERROR_STOP=1 -c $sql
    if ($LASTEXITCODE -ne 0) { throw "psql reset failed with exit code $LASTEXITCODE" }

    & $PsqlPath -h $HostName -p $Port -U $AdminUser -d $Database -c "select rolname from pg_roles where rolname in ('s101_xanhnow_auth_security_migrator','s101_xanhnow_auth_security_runtime','xanhnow_security_migrator','xanhnow_security_api','xanhnow_security_worker','xanhnow_security') order by rolname; select schema_name from information_schema.schemata where schema_name = 'security';"
    if ($LASTEXITCODE -ne 0) { throw "psql verification failed with exit code $LASTEXITCODE" }

    Write-Host "XanhNow Security PostgreSQL legacy schema/data/roles reset completed."
}
finally {
    Remove-Item Env:\PGSSLMODE -ErrorAction SilentlyContinue
    Remove-Item Env:\PGSSLROOTCERT -ErrorAction SilentlyContinue
}
