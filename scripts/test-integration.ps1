param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$resultsRoot = Join-Path $repoRoot "artifacts/test-results/integration"
New-Item -ItemType Directory -Force -Path $resultsRoot | Out-Null

$projects = @(
    "tests/XanhNow.Security.IntegrationTests/XanhNow.Security.IntegrationTests.csproj",
    "tests/XanhNow.Security.EndToEndTests/XanhNow.Security.EndToEndTests.csproj"
)

Push-Location $repoRoot
try {
    foreach ($project in $projects) {
        $name = [System.IO.Path]::GetFileNameWithoutExtension($project)
        $projectResults = Join-Path $resultsRoot $name
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
