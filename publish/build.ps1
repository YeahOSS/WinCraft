[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$ProjectPath,
    [switch]$BuildOnly
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Import-Module (Join-Path $PSScriptRoot "modules\common.psm1") -Force
Import-Module (Join-Path $PSScriptRoot "modules\overlay.psm1") -Force
Import-Module (Join-Path $PSScriptRoot "modules\installer.psm1") -Force

$script:PublishRoot = $PSScriptRoot
$script:RepositoryRoot = Split-Path -Parent $script:PublishRoot
$script:SourceRoot = Join-Path $script:RepositoryRoot "src"
$script:PublishOutputPath = Join-Path $script:PublishRoot "output"
$script:LegacyArtifactName = "WinCraft-Legacy.exe"
$script:StandardArtifactName = "WinCraft-Standard.exe"
$script:FullInstallerArtifactName = "WinCraft-Setup.exe"
$script:ResolvedProjectPath = $null
$script:ProjectRoot = $null

function Resolve-ProjectFilePath {
    if (-not [string]::IsNullOrWhiteSpace($ProjectPath)) {
        $resolvedProjectPath = Resolve-Path -LiteralPath $ProjectPath -ErrorAction Stop
        return $resolvedProjectPath.Path
    }

    $projectFiles = @(Get-ChildItem -Path $script:SourceRoot -Filter WinCraft.csproj -Recurse | Select-Object -ExpandProperty FullName)

    if ($projectFiles.Count -eq 0) {
        throw "No project file was found under the src directory."
    }

    if ($projectFiles.Count -gt 1) {
        throw "Multiple project files were found under src. Pass -ProjectPath to choose the target project."
    }

    return $projectFiles[0]
}

function Get-MSBuildCommand {
    $programFilesX86 = ${env:ProgramFiles(x86)}
    $vsWherePath = $null

    if (-not [string]::IsNullOrEmpty($programFilesX86)) {
        $vsWherePath = Join-Path $programFilesX86 "Microsoft Visual Studio\Installer\vswhere.exe"
    }

    if (($null -ne $vsWherePath) -and (Test-Path -LiteralPath $vsWherePath)) {
        $installationPath = & $vsWherePath -latest -products * -requires Microsoft.Component.MSBuild -property installationPath

        if ($LASTEXITCODE -ne 0) {
            throw "Failed to locate MSBuild by using vswhere."
        }

        if (-not [string]::IsNullOrWhiteSpace($installationPath)) {
            $msbuildPath = Join-Path $installationPath.Trim() "MSBuild\Current\Bin\MSBuild.exe"

            if (Test-Path -LiteralPath $msbuildPath) {
                return @{
                    Type = "MSBuild"
                    Path = $msbuildPath
                }
            }
        }
    }

    $dotnetCommand = Get-Command dotnet -ErrorAction SilentlyContinue

    if ($null -ne $dotnetCommand) {
        return @{
            Type = "DotNet"
            Path = $dotnetCommand.Source
        }
    }

    throw "No usable MSBuild.exe or dotnet msbuild command was found."
}

function Test-TargetingPack {
    param(
        [string[]]$AssemblyRelativePaths
    )

    $programFilesX86 = ${env:ProgramFiles(x86)}

    if ([string]::IsNullOrEmpty($programFilesX86)) {
        return $false
    }

    foreach ($assemblyRelativePath in $AssemblyRelativePaths) {
        if (-not [string]::IsNullOrWhiteSpace($assemblyRelativePath)) {
            $assemblyPath = Join-Path $programFilesX86 $assemblyRelativePath

            if (Test-Path -LiteralPath $assemblyPath) {
                return $true
            }
        }
    }

    return $false
}

function Assert-BuildPrerequisites {
    Assert-PathExists -Path $script:ResolvedProjectPath -Description "Project file"

    if (-not (Test-TargetingPack -AssemblyRelativePaths @(
        "Reference Assemblies\Microsoft\Framework\v3.0\PresentationFramework.dll",
        "Reference Assemblies\Microsoft\Framework\.NETFramework\v3.0\PresentationFramework.dll",
        "Reference Assemblies\Microsoft\Framework\v3.5\Profile\Client\System.dll"
    ))) {
        throw "The .NET Framework 3.0 build prerequisites were not found. Install the matching Visual Studio components or targeting pack first."
    }

    if (-not (Test-TargetingPack -AssemblyRelativePaths @(
        "Reference Assemblies\Microsoft\Framework\.NETFramework\v4.5\mscorlib.dll"
    ))) {
        throw "The .NET Framework 4.5 targeting pack was not found. Install the matching Developer Pack or Targeting Pack first."
    }
}

function Invoke-ProjectRestore {
    param(
        [hashtable]$Builder,
        [string]$ProjectPath
    )

    Write-Step "Restoring project"

    if ($Builder.Type -eq "MSBuild") {
        & $Builder.Path $ProjectPath "/nologo" "/verbosity:minimal" "/t:Restore" | Out-Host
    }
    else {
        & $Builder.Path "msbuild" $ProjectPath "/nologo" "/verbosity:minimal" "/t:Restore" | Out-Host
    }

    if ($LASTEXITCODE -ne 0) {
        throw "Project restore failed."
    }
}

function Invoke-ProjectBuild {
    param(
        [hashtable]$Builder,
        [string]$ProjectPath,
        [string]$ProjectLabel,
        [string[]]$ExtraBuildProperties = @()
    )

    Write-Step "Building $ProjectLabel"

    # Pass ContinuousIntegrationBuild for Release to enable full deterministic build semantics.
    # Net30ValidationBuild suppresses the post-build validation target since this
    # script already builds both TFMs via VS MSBuild.
    $extraProperties = @("/p:Net30ValidationBuild=true")
    if ($Configuration -eq "Release") {
        $extraProperties += "/p:ContinuousIntegrationBuild=true"
    }
    $extraProperties += $ExtraBuildProperties

    if ($Builder.Type -eq "MSBuild") {
        & $Builder.Path $ProjectPath "/nologo" "/verbosity:minimal" "/p:Configuration=$Configuration" $extraProperties "/t:Build" | Out-Host
    }
    else {
        & $Builder.Path "msbuild" $ProjectPath "/nologo" "/verbosity:minimal" "/p:Configuration=$Configuration" $extraProperties "/t:Build" | Out-Host
    }

    if ($LASTEXITCODE -ne 0) {
        throw "$ProjectLabel build failed."
    }
}

Write-Step "Resolving build inputs"
$script:ResolvedProjectPath = Resolve-ProjectFilePath
$script:ProjectRoot = Split-Path -Parent $script:ResolvedProjectPath

Write-Step "Checking build prerequisites"
$builder = Get-MSBuildCommand
Assert-BuildPrerequisites

Invoke-ProjectRestore -Builder $builder -ProjectPath $script:ResolvedProjectPath

Write-Step "Preparing the publish output directory"

if (Test-Path -LiteralPath $script:PublishOutputPath) {
    Remove-Item -LiteralPath $script:PublishOutputPath -Recurse -Force
}

New-Item -ItemType Directory -Path $script:PublishOutputPath -Force | Out-Null

# First build — with overlay/resolver code for standalone single-file EXEs.
Invoke-ProjectBuild -Builder $builder -ProjectPath $script:ResolvedProjectPath -ProjectLabel "standalone"

if (-not $BuildOnly) {
    New-OverlayExe -BuildLabel "net45" -Configuration $Configuration -ProjectRoot $script:ProjectRoot -TargetSubdirectory "net45" -ArtifactName $script:StandardArtifactName | Out-Null
    New-OverlayExe -BuildLabel "net30" -Configuration $Configuration -ProjectRoot $script:ProjectRoot -TargetSubdirectory "net30" -ArtifactName $script:LegacyArtifactName | Out-Null

    # Second build — without overlay/resolver code for the NSIS installer.
    Invoke-ProjectBuild -Builder $builder -ProjectPath $script:ResolvedProjectPath -ProjectLabel "installer" -ExtraBuildProperties @("/p:InstallerBuild=true")

    New-NSISInstaller -Configuration $Configuration -ProjectRoot $script:ProjectRoot -ArtifactName $script:FullInstallerArtifactName | Out-Null

    Write-Step "Build completed"
    foreach ($artifact in @($script:LegacyArtifactName, $script:StandardArtifactName, $script:FullInstallerArtifactName)) {
        $artifactPath = Join-Path $script:PublishOutputPath $artifact
        if (Test-Path -LiteralPath $artifactPath) {
            $size = [math]::Round((Get-Item -LiteralPath $artifactPath).Length / 1KB, 1)
            Write-Host "  $artifact ($size KB)"
        }
    }
}
