<#
.SYNOPSIS
Quick build validation for both target frameworks.
Uses VS MSBuild to support net30 and net45.
#>
[CmdletBinding()]
param(
    [string]$Configuration = "Debug",
    [switch]$Test
)

$ErrorActionPreference = "Stop"
$srcRoot = $PSScriptRoot

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

# Build
# Pass Net30ValidationBuild=true so post-build events skip re-entrant validation.
$projectFile = Join-Path $srcRoot "WinCraft\WinCraft.csproj"
if (-not (Test-Path $projectFile)) {
    throw "Main project not found at $projectFile"
}
Write-Host "Building ($Configuration) ..." -ForegroundColor Cyan
& $msbuild $projectFile /t:Restore /nologo /verbosity:quiet /p:Net30ValidationBuild=true
if ($LASTEXITCODE -ne 0) { throw "Restore failed." }
& $msbuild $projectFile /t:Build /p:Configuration=$Configuration /nologo /verbosity:minimal /p:Net30ValidationBuild=true
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

# Test
if ($Test) {
    $testBin = Join-Path $srcRoot "WinCraft.Tests\bin\$Configuration\net45\WinCraft.Tests.exe"
    if (Test-Path $testBin) {
        Write-Host "Running tests ..." -ForegroundColor Cyan
        & $testBin
    }
}

Write-Host "OK" -ForegroundColor Green
