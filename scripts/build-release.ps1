param(
    [string]$Configuration = "Release",
    [string]$Version = "local",
    [string]$CommitSha = "local",
    [string]$OutputRoot = "artifacts/release"
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$releaseId = "xanhnow-security-$Version-$CommitSha"
$releaseRoot = Join-Path (Join-Path $repoRoot $OutputRoot) $releaseId
$publishRoot = Join-Path $releaseRoot "publish"

$projects = @(
    @{ Name = "api"; Project = "src/XanhNow.Security.Api/XanhNow.Security.Api.csproj" },
    @{ Name = "worker"; Project = "src/XanhNow.Security.Worker/XanhNow.Security.Worker.csproj" },
    @{ Name = "migrator"; Project = "src/XanhNow.Security.Migrator/XanhNow.Security.Migrator.csproj" }
)

if (Test-Path $releaseRoot) {
    Remove-Item -Recurse -Force $releaseRoot
}

New-Item -ItemType Directory -Force -Path $publishRoot | Out-Null
Push-Location $repoRoot
try {
    dotnet restore XanhNow.Security.slnx --disable-parallel

    foreach ($item in $projects) {
        $destination = Join-Path $publishRoot $item.Name
        dotnet publish $item.Project -c $Configuration --no-restore -o $destination
    }

    $manifest = [ordered]@{
        app = "XanhNow.Security"
        version = $Version
        commitSha = $CommitSha
        releaseId = $releaseId
        generatedAtUtc = (Get-Date).ToUniversalTime().ToString("O")
        artifacts = $projects | ForEach-Object { [ordered]@{ name = $_.Name; path = "publish/$($_.Name)" } }
    }

    $manifest | ConvertTo-Json -Depth 6 | Set-Content -Encoding UTF8 (Join-Path $releaseRoot "release.json")

    $checksumFile = Join-Path $releaseRoot "SHA256SUMS.txt"
    Get-ChildItem -Path $publishRoot -Recurse -File |
        Sort-Object FullName |
        ForEach-Object {
            $relative = $_.FullName.Substring($releaseRoot.Length).TrimStart("\", "/").Replace("\", "/")
            $hash = (Get-FileHash -Algorithm SHA256 -Path $_.FullName).Hash.ToLowerInvariant()
            "$hash  $relative"
        } | Set-Content -Encoding ASCII $checksumFile

    foreach ($item in $projects) {
        $source = Join-Path $publishRoot $item.Name
        $archive = Join-Path $releaseRoot ("$($item.Name).zip")
        Compress-Archive -Path (Join-Path $source "*") -DestinationPath $archive -Force
    }

    Write-Host "RB17_RELEASE_ROOT=$releaseRoot"
}
finally {
    Pop-Location
}

