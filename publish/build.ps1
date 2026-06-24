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

# Builds a single-file executable by appending a compressed zip of dependency
# DLLs as a PE overlay.  The PE header and sections stay untouched — the data
# lives after the last section where the PE loader ignores it.
# At runtime OverlayAssemblyResolver (Program.cs) reads the overlay, decompresses
# the zip, and serves assemblies from memory on demand.
function New-OverlayExe {
    param(
        [string]$BuildLabel,
        [string]$ProjectRoot,
        [string]$TargetSubdirectory,
        [string]$ArtifactName
    )

    Write-Step "Building $BuildLabel single-file executable with dependency overlay"

    $buildOutputDirectory = Get-BuildOutputDirectory -ProjectRoot $ProjectRoot -TargetSubdirectory $TargetSubdirectory
    $exePath = Join-Path $buildOutputDirectory "WinCraft.exe"
    $artifactPath = Join-Path $script:PublishOutputPath $ArtifactName

    Assert-PathExists -Path $exePath -Description "$BuildLabel executable"

    # Collect all .dll files in the build output.
    $dllPaths = @(Get-ChildItem -Path $buildOutputDirectory -Filter "*.dll" -File | Select-Object -ExpandProperty FullName)
    $dllNames = $dllPaths | ForEach-Object { Split-Path $_ -Leaf }
    Write-Host "Dependencies: $($dllNames -join ', ')"

    # Build a flat container: [int32 count] (for each: [int16 nameLen] [name] [int32 dataLen] [data]).
    $containerStream = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter($containerStream, [System.Text.Encoding]::UTF8)
    $writer.Write([int32]$dllPaths.Count)
    $totalDllKB = 0
    foreach ($dllPath in $dllPaths) {
        $name = Split-Path $dllPath -Leaf
        $nameBytes = [System.Text.Encoding]::UTF8.GetBytes($name)
        $data = [System.IO.File]::ReadAllBytes($dllPath)
        $totalDllKB += [math]::Round($data.Length / 1KB, 0)
        $writer.Write([int16]$nameBytes.Length)
        $writer.Write($nameBytes)
        $writer.Write([int32]$data.Length)
        $writer.Write($data)
    }
    $writer.Flush()
    $rawBytes = $containerStream.ToArray()
    $writer.Dispose()
    $containerStream.Dispose()

    $containerKB = [math]::Round($rawBytes.Length / 1KB, 0)

    # Deflate-compress the container.
    $compressedStream = New-Object System.IO.MemoryStream
    $deflate = New-Object System.IO.Compression.DeflateStream(
        $compressedStream,
        [System.IO.Compression.CompressionLevel]::Optimal,
        $true)
    $deflate.Write($rawBytes, 0, $rawBytes.Length)
    $deflate.Close()
    $compressedBytes = $compressedStream.ToArray()
    $deflate.Dispose()
    $compressedStream.Dispose()

    $compressedKB = [math]::Round($compressedBytes.Length / 1KB, 0)
    Write-Host "DLLs $totalDllKB KB raw -> $containerKB KB container -> $compressedKB KB Deflate"

    # Append overlay: [compressed data] [4 bytes LE: compressed len] [4 bytes: magic "WOVL"]
    $magic = [System.BitConverter]::GetBytes([uint32]0x4C564F57)
    $lenBytes = [System.BitConverter]::GetBytes([int32]$compressedBytes.Length)

    if (Test-Path -LiteralPath $artifactPath) {
        Remove-Item -LiteralPath $artifactPath -Force
    }

    Copy-Item -LiteralPath $exePath -Destination $artifactPath -Force

    $exeBytes = [System.IO.File]::ReadAllBytes($artifactPath)
    $exeBaseKB = [math]::Round($exeBytes.Length / 1KB, 0)
    $footerSize = 8
    $overlayOffset = $exeBytes.Length
    [Array]::Resize([ref]$exeBytes, $overlayOffset + $compressedBytes.Length + $footerSize)
    [Array]::Copy($compressedBytes, 0, $exeBytes, $overlayOffset, $compressedBytes.Length)
    [Array]::Copy($lenBytes, 0, $exeBytes, $overlayOffset + $compressedBytes.Length, 4)
    [Array]::Copy($magic, 0, $exeBytes, $overlayOffset + $compressedBytes.Length + 4, 4)
    [System.IO.File]::WriteAllBytes($artifactPath, $exeBytes)

    $finalKB = [math]::Round($exeBytes.Length / 1KB, 0)
    Write-Host "$($BuildLabel): $exeBaseKB KB exe + $compressedKB KB overlay = $finalKB KB single-file"

    Assert-PathExists -Path $artifactPath -Description "$BuildLabel single-file artifact"

    return [pscustomobject]@{
        ArtifactPath = $artifactPath
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

Invoke-ProjectBuild -Builder $builder -ProjectPath $script:ResolvedProjectPath -ProjectLabel "all framework variants"

# Both targets use PE overlay — clean PE, no AV false positives.
$standardBuildResult = New-OverlayExe -BuildLabel "net45" -ProjectRoot $script:ProjectRoot -TargetSubdirectory "net45" -ArtifactName $script:StandardArtifactName
$legacyBuildResult = New-OverlayExe -BuildLabel "net30" -ProjectRoot $script:ProjectRoot -TargetSubdirectory "net30" -ArtifactName $script:LegacyArtifactName

Write-Step "Packaging Full distribution"
$zipPath = Join-Path $script:PublishOutputPath $script:FullArtifactName
$configPath = Join-Path (Get-BuildOutputDirectory -ProjectRoot $script:ProjectRoot -TargetSubdirectory "net30") "WinCraft.exe.config"
Assert-PathExists -Path $configPath -Description "Legacy config file"

# The artifact is named WinCraft-Legacy.exe; the CLR discovers the runtime
# config by replacing the .exe extension with .exe.config, so the config
# must be renamed to match the artifact.
$legacyExePath = $legacyBuildResult.ArtifactPath
$legacyConfigPath = $legacyExePath + ".config"
Copy-Item -LiteralPath $configPath -Destination $legacyConfigPath -Force

if (Test-Path -LiteralPath $zipPath) { Remove-Item -LiteralPath $zipPath -Force }
Compress-Archive -LiteralPath $legacyExePath, $legacyConfigPath -DestinationPath $zipPath
Assert-PathExists -Path $zipPath -Description "Full package"

Remove-Item -LiteralPath $legacyConfigPath -Force

Write-Step "Build completed"
$standardSizeKB = [math]::Round((Get-Item $standardBuildResult.ArtifactPath).Length / 1KB, 0)
$legacySizeKB = [math]::Round((Get-Item $legacyBuildResult.ArtifactPath).Length / 1KB, 0)
$fullSizeKB = [math]::Round((Get-Item $zipPath).Length / 1KB, 0)
Write-Host "Standard: $($standardBuildResult.ArtifactPath) ($standardSizeKB KB)"
Write-Host "Legacy: $($legacyBuildResult.ArtifactPath) ($legacySizeKB KB)"
Write-Host "Full: $zipPath ($fullSizeKB KB)"
