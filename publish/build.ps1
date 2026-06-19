[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$ProjectPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Decode native-command output (MSBuild) as UTF-8 to avoid garbled characters
# when the system console code page differs from the tool output encoding.
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$script:PublishRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$script:RepositoryRoot = Split-Path -Parent $script:PublishRoot
$script:SourceRoot = Join-Path $script:RepositoryRoot "src"
$script:PublishOutputPath = Join-Path $script:PublishRoot "output"
$script:LegacyArtifactName = "WinCraft-Legacy.exe"
$script:StandardArtifactName = "WinCraft-Standard.exe"
$script:FullArtifactName = "WinCraft-Full.zip"
$script:ResolvedProjectPath = $null
$script:ProjectRoot = $null

function Write-Step {
    param(
        [string]$Message
    )

    Write-Host ""
    Write-Host "==> $Message"
}

function Assert-PathExists {
    param(
        [string]$Path,
        [string]$Description
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        throw "$Description was not found: $Path"
    }
}

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
        [string]$ProjectLabel
    )

    Write-Step "Building $ProjectLabel"

    # Pass ContinuousIntegrationBuild for Release to enable full deterministic build semantics.
    $extraProperties = @()
    if ($Configuration -eq "Release") {
        $extraProperties += "/p:ContinuousIntegrationBuild=true"
    }

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

function Get-BuildOutputDirectory {
    param(
        [string]$ProjectRoot,
        [string]$TargetSubdirectory
    )

    return Join-Path $ProjectRoot "bin\$Configuration\$TargetSubdirectory"
}

function Publish-SingleFileArtifact {
    param(
        [string]$BuildLabel,
        [string]$ProjectRoot,
        [string]$TargetSubdirectory,
        [string]$ArtifactName
    )

    Write-Step "Collecting the $BuildLabel single-file executable"

    $buildOutputDirectory = Get-BuildOutputDirectory -ProjectRoot $ProjectRoot -TargetSubdirectory $TargetSubdirectory
    $mergedExePath = Join-Path $buildOutputDirectory "merged\WinCraft.exe"
    $artifactPath = Join-Path $script:PublishOutputPath $ArtifactName

    Assert-PathExists -Path $buildOutputDirectory -Description "$BuildLabel output directory"
    Assert-PathExists -Path $mergedExePath -Description "$BuildLabel merged executable"

    if (Test-Path -LiteralPath $artifactPath) {
        Remove-Item -LiteralPath $artifactPath -Force
    }

    Copy-Item -LiteralPath $mergedExePath -Destination $artifactPath -Force
    Assert-PathExists -Path $artifactPath -Description "$BuildLabel single-file artifact"

    return [pscustomobject]@{
        BuildOutputDirectory = $buildOutputDirectory
        ArtifactPath = $artifactPath
    }
}

function New-FullPackage {
    Write-Step "Creating the Full package"

    $zipPath = Join-Path $script:PublishOutputPath $script:FullArtifactName
    $legacyBuildOutputDirectory = Get-BuildOutputDirectory -ProjectRoot $script:ProjectRoot -TargetSubdirectory "net30"
    $legacyExePath = Join-Path $legacyBuildOutputDirectory "merged\WinCraft.exe"
    $configPath = Join-Path $legacyBuildOutputDirectory "WinCraft.exe.config"

    Assert-PathExists -Path $legacyExePath -Description "Legacy merged executable"
    Assert-PathExists -Path $configPath -Description "Legacy configuration file"

    if (Test-Path -LiteralPath $zipPath) {
        Remove-Item -LiteralPath $zipPath -Force
    }

    Compress-Archive -LiteralPath $legacyExePath, $configPath -DestinationPath $zipPath
    Assert-PathExists -Path $zipPath -Description "Full package archive"
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

Invoke-ProjectBuild -Builder $builder -ProjectPath $script:ResolvedProjectPath -ProjectLabel "all framework variants"

$legacyBuildResult = Publish-SingleFileArtifact -BuildLabel "net30" -ProjectRoot $script:ProjectRoot -TargetSubdirectory "net30" -ArtifactName $script:LegacyArtifactName
$standardBuildResult = Publish-SingleFileArtifact -BuildLabel "net45" -ProjectRoot $script:ProjectRoot -TargetSubdirectory "net45" -ArtifactName $script:StandardArtifactName

New-FullPackage

Write-Step "Build completed"
Write-Host "Legacy: $($legacyBuildResult.ArtifactPath)"
Write-Host "Standard: $($standardBuildResult.ArtifactPath)"
Write-Host "Full: $(Join-Path $script:PublishOutputPath $script:FullArtifactName)"
