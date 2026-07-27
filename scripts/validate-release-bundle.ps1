param(
    [Parameter(Mandatory = $true)]
    [string]$ReleaseRoot
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

$requiredPaths = @(
    "release.json",
    "SHA256SUMS.txt",
    "publish/api",
    "publish/worker",
    "publish/migrator",
    "api.zip",
    "worker.zip",
    "migrator.zip"
)

foreach ($path in $requiredPaths) {
    $fullPath = Join-Path $ReleaseRoot $path
    if (-not (Test-Path $fullPath)) {
        throw "Missing release bundle path: $path"
    }
}

$manifest = Get-Content -Raw (Join-Path $ReleaseRoot "release.json") | ConvertFrom-Json
if ($manifest.app -ne "XanhNow.Security") {
    throw "Invalid app name in release.json"
}

$checksumFile = Join-Path $ReleaseRoot "SHA256SUMS.txt"
$lines = Get-Content $checksumFile
if ($lines.Count -eq 0) {
    throw "SHA256SUMS.txt is empty"
}

foreach ($line in $lines) {
    if ($line -notmatch "^([a-f0-9]{64})\s\s(.+)$") {
        throw "Invalid checksum line: $line"
    }

    $expected = $Matches[1]
    $relative = $Matches[2]
    $file = Join-Path $ReleaseRoot $relative
    if (-not (Test-Path $file)) {
        throw "Checksum references missing file: $relative"
    }

    $actual = (Get-FileHash -Algorithm SHA256 -Path $file).Hash.ToLowerInvariant()
    if ($actual -ne $expected) {
        throw "Checksum mismatch: $relative"
    }
}

Write-Host "RB17_RELEASE_BUNDLE_VALID=$ReleaseRoot"
