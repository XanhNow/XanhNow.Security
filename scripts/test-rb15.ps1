param(
    [string]$Configuration = "Release",
    [switch]$EnableExternalFixtures
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$resultsRoot = Join-Path $repoRoot "artifacts/test-results/rb15"
New-Item -ItemType Directory -Force -Path $resultsRoot | Out-Null

if ($EnableExternalFixtures) {
    $env:XANHNOW_SECURITY_ENABLE_EXTERNAL_FIXTURES = "1"
} else {
    Remove-Item Env:\XANHNOW_SECURITY_ENABLE_EXTERNAL_FIXTURES -ErrorAction SilentlyContinue
}

$projects = @(
    "tests/XanhNow.Security.ContractTests/XanhNow.Security.ContractTests.csproj",
    "tests/XanhNow.Security.IntegrationTests/XanhNow.Security.IntegrationTests.csproj",
    "tests/XanhNow.Security.EndToEndTests/XanhNow.Security.EndToEndTests.csproj"
)

Push-Location $repoRoot
try {
    foreach ($project in $projects) {
        $projectName = [System.IO.Path]::GetFileNameWithoutExtension($project)
        $projectResults = Join-Path $resultsRoot $projectName
        New-Item -ItemType Directory -Force -Path $projectResults | Out-Null

        dotnet test $project `
            -c $Configuration `
            --no-restore `
            --settings .\tests\XanhNow.Security.runsettings `
            --logger "trx" `
            --results-directory $projectResults
    }
}
finally {
    Pop-Location
}
