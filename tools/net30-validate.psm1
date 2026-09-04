# Shared net30 validation helpers.  Imported inline by per-project ValidateNet30
# MSBuild targets via `powershell -Command "Import-Module '...'; Invoke-Net30Build ..."`.

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$script:RepoRoot = Split-Path $PSScriptRoot -Parent

function Get-MSBuild {
    $vsWhere = Join-Path ${env:ProgramFiles(x86)} "Microsoft Visual Studio\Installer\vswhere.exe"
    if (-not (Test-Path $vsWhere)) {
        throw "vswhere.exe not found. Install Visual Studio."
    }
    $vsPath = & $vsWhere -latest -products * -requires Microsoft.Component.MSBuild -property installationPath
    if ($LASTEXITCODE -ne 0 -or -not $vsPath) {
        throw "Could not locate Visual Studio with MSBuild."
    }
    return Join-Path $vsPath.Trim() "MSBuild\Current\Bin\MSBuild.exe"
}

function Invoke-Net30Build {
    param(
        [Parameter(Mandatory)]
        [string]$ProjectPath,
        [string]$Configuration = "Debug",
        [switch]$SkipProjectReferences
    )
    $projectFile = Join-Path $script:RepoRoot $ProjectPath
    $projectName = [System.IO.Path]::GetFileNameWithoutExtension($projectFile)

    Write-Host "Validating net30 ..." -ForegroundColor Cyan
    Write-Host "  $projectName"

    $extraArgs = @()
    if ($SkipProjectReferences) {
        $extraArgs += "/p:BuildProjectReferences=false"
    }

    $msbuild = Get-MSBuild
    $msbuildArgs = @(
        $projectFile,
        "/t:Build",
        "/p:Configuration=$Configuration",
        "/p:TargetFramework=net30",
        "/nologo",
        "/verbosity:minimal",
        "/p:Net30ValidationBuild=true"
    ) + $extraArgs

    $proc = Start-Process -FilePath $msbuild -ArgumentList $msbuildArgs -Wait -NoNewWindow -PassThru
    if ($proc.ExitCode -ne 0) { throw "$projectName build failed." }

    # Windows releases file handles asynchronously after process exit.
    # The outer dotnet build may copy net45 outputs immediately after
    # this script returns, so give the OS a moment to clean up handles
    # held by the just-exited MSBuild process (MSB3026).
    Start-Sleep -Milliseconds 500

    Write-Host "OK" -ForegroundColor Green
}
