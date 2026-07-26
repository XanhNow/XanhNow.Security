param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$resultsRoot = Join-Path $repoRoot "artifacts/test-results/fast"
New-Item -ItemType Directory -Force -Path $resultsRoot | Out-Null

$projects = @(
    "tests/XanhNow.Security.Domain.Tests/XanhNow.Security.Domain.Tests.csproj",
    "tests/XanhNow.Security.Application.Tests/XanhNow.Security.Application.Tests.csproj",
    "tests/XanhNow.Security.ArchitectureTests/XanhNow.Security.ArchitectureTests.csproj",
    "tests/XanhNow.Security.ContractTests/XanhNow.Security.ContractTests.csproj",
    "tests/XanhNow.Security.Api.Tests/XanhNow.Security.Api.Tests.csproj",
    "tests/XanhNow.Security.Worker.Tests/XanhNow.Security.Worker.Tests.csproj",
    "tests/XanhNow.Security.Migrator.Tests/XanhNow.Security.Migrator.Tests.csproj"
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
