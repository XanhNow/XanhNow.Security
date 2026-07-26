param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$results = Join-Path $repoRoot "artifacts/test-results/all"
New-Item -ItemType Directory -Force -Path $results | Out-Null

Push-Location $repoRoot
try {
    dotnet test .\XanhNow.Security.slnx `
        -c $Configuration `
        --settings .\tests\XanhNow.Security.runsettings `
        --logger "trx" `
        --results-directory $results
}
finally {
    Pop-Location
}
