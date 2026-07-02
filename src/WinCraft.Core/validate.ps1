<#
.SYNOPSIS
Validates net30 build via VS MSBuild (dotnet SDK cannot resolve net30).
Called by Core post-build after dotnet build completes net45.
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Debug",
    [switch]$Test
)

$ErrorActionPreference = "Stop"
$srcRoot = Split-Path $PSScriptRoot -Parent

# Find VS MSBuild
$vsWhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
if (-not (Test-Path $vsWhere)) {
    throw "vswhere.exe not found. Install Visual Studio."
}
$vsPath = & $vsWhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
if ($LASTEXITCODE -ne 0 -or -not $vsPath) {
    throw "Could not locate Visual Studio with MSBuild."
}
$msbuild = Join-Path $vsPath.Trim() "MSBuild\Current\Bin\MSBuild.exe"

# Build both TFMs via VS MSBuild — net45 is incremental, net30 is the validation.
# Net30ValidationBuild=true prevents the post-build event from re-entering.
$projectFile = Join-Path $srcRoot "WinCraft\WinCraft.csproj"
Write-Host "Validating net30 ..." -ForegroundColor Cyan
& $msbuild $projectFile /t:Build /p:Configuration=$Configuration /nologo /verbosity:minimal /p:Net30ValidationBuild=true
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

# Test
if ($Test) {
    $testBin = Join-Path $srcRoot "bin\$Configuration\net45\WinCraft.Tests.exe"
    if (Test-Path $testBin) {
        Write-Host "Running tests ..." -ForegroundColor Cyan
        & $testBin
    }
}

Write-Host "OK" -ForegroundColor Green
