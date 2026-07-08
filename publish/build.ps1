[CmdletBinding()]
param(
    [string]$Configuration = "Release",
    [string]$ProjectPath,
    [string]$StandaloneProjectPath,
    [string]$InstallerProjectPath,
    [switch]$BuildOnly,
    [switch]$SkipNSIS,
    [switch]$SkipMSI
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Import-Module (Join-Path $PSScriptRoot "modules\common.psm1") -Force
Set-ProcessPathEnvironment
Import-Module (Join-Path $PSScriptRoot "modules\overlay.psm1") -Force
Import-Module (Join-Path $PSScriptRoot "modules\nsis.psm1") -Force

$script:PublishRoot = $PSScriptRoot
$script:RepositoryRoot = Split-Path -Parent $script:PublishRoot
$script:SourceRoot = Join-Path $script:RepositoryRoot "src"
$script:PublishOutputPath = Join-Path $script:PublishRoot "output"
$script:PublishStagingPath = Join-Path $script:PublishOutputPath "staging"
$script:LegacyArtifactName = "WinCraft-Legacy.exe"
$script:StandardArtifactName = "WinCraft-Standard.exe"
$script:FullInstallerArtifactName = "WinCraft-Setup.exe"
$script:MSIArtifactName = "WinCraft-Setup.msi"
$script:OverlayStats = @{}

function Clear-PublishStagingDirectory {
    if (Test-Path -LiteralPath $script:PublishStagingPath) {
        Remove-Item -LiteralPath $script:PublishStagingPath -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Clear-BuildOutputDirectories {
    $outputRoot = [System.IO.Path]::GetFullPath((Join-Path $script:SourceRoot "bin"))
    $outputRootPrefix = $outputRoot + [System.IO.Path]::DirectorySeparatorChar

    foreach ($targetFramework in @("net30", "net45")) {
        $targetPath = [System.IO.Path]::GetFullPath((Join-Path $outputRoot "$Configuration\$targetFramework"))

        if (-not $targetPath.StartsWith($outputRootPrefix, [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "Refusing to clean build output outside src\bin: $targetPath"
        }

        if (Test-Path -LiteralPath $targetPath) {
            Remove-Item -LiteralPath $targetPath -Recurse -Force -ErrorAction Stop
        }
    }
}

function Clear-OutputFile {
    param(
        [string]$ArtifactName
    )

    $path = Join-Path $script:PublishOutputPath $ArtifactName
    if (-not (Test-Path -LiteralPath $path)) {
        return $true
    }

    try {
        Remove-Item -LiteralPath $path -Force -ErrorAction Stop
        return $true
    }
    catch {
        return $false
    }
}

function Resolve-ProjectFilePath {
    param(
        [string]$ProjectFileName,
        [string]$ExplicitProjectPath = ""
    )

    if (-not [string]::IsNullOrWhiteSpace($ExplicitProjectPath)) {
        $resolvedProjectPath = Resolve-Path -LiteralPath $ExplicitProjectPath -ErrorAction Stop
        return $resolvedProjectPath.Path
    }

    $projectFiles = @(Get-ChildItem -Path $script:SourceRoot -Filter $ProjectFileName -Recurse |
        Select-Object -ExpandProperty FullName)

    if ($projectFiles.Count -eq 0) {
        throw "No $ProjectFileName file was found under the src directory."
    }

    if ($projectFiles.Count -gt 1) {
        throw "Multiple $ProjectFileName files were found under src."
    }

    return $projectFiles[0]
}

function Resolve-BuildProjects {
    if (-not [string]::IsNullOrWhiteSpace($ProjectPath)) {
        if (-not [string]::IsNullOrWhiteSpace($StandaloneProjectPath)) {
            throw "Use either -ProjectPath or -StandaloneProjectPath, not both."
        }

        $StandaloneProjectPath = $ProjectPath
    }

    $resolvedStandaloneProjectPath = Resolve-ProjectFilePath `
        -ProjectFileName "WinCraft.Portable.csproj" `
        -ExplicitProjectPath $StandaloneProjectPath
    $resolvedInstallerProjectPath = Resolve-ProjectFilePath `
        -ProjectFileName "WinCraft.csproj" `
        -ExplicitProjectPath $InstallerProjectPath

    return [pscustomobject]@{
        StandaloneProjectPath = $resolvedStandaloneProjectPath
        StandaloneProjectRoot = Split-Path -Parent $resolvedStandaloneProjectPath
        InstallerProjectPath  = $resolvedInstallerProjectPath
        InstallerProjectRoot  = Split-Path -Parent $resolvedInstallerProjectPath
    }
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
    param(
        [object]$Projects
    )

    Assert-PathExists -Path $Projects.StandaloneProjectPath -Description "Standalone project file"
    Assert-PathExists -Path $Projects.InstallerProjectPath -Description "Installer project file"

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

function Invoke-BuildCommand {
    param(
        [hashtable]$Builder,
        [string[]]$Arguments
    )

    if ($Builder.Type -eq "MSBuild") {
        $output = & $Builder.Path @Arguments 2>&1
    }
    else {
        $output = & $Builder.Path "msbuild" @Arguments 2>&1
    }

    return @{
        ExitCode = $LASTEXITCODE
        Output   = $output
    }
}

function Invoke-ProjectRestore {
    param(
        [hashtable]$Builder,
        [string]$ProjectPath,
        [string]$ProjectLabel,
        [string[]]$ExtraRestoreProperties = @()
    )

    Write-Step "Restoring $ProjectLabel"

    $restoreArguments = @(
        $ProjectPath,
        "/nologo",
        "/verbosity:quiet",
        "/p:NuGetAudit=false",
        "/p:RestoreForceEvaluate=true",
        "/p:WarningsNotAsErrors=NU1900",
        "/t:Restore"
    ) + $ExtraRestoreProperties

    $result = Invoke-BuildCommand -Builder $Builder -Arguments $restoreArguments

    if ($result.ExitCode -ne 0) {
        Write-Host ($result.Output | Out-String)
        throw "$ProjectLabel restore failed."
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

    $buildProperties = @(
        "/p:Configuration=$Configuration",
        "/p:Net30ValidationBuild=true"
    )
    if ($Configuration -eq "Release") {
        $buildProperties += "/p:ContinuousIntegrationBuild=true"
    }
    $buildProperties += $ExtraBuildProperties

    $result = Invoke-BuildCommand -Builder $Builder -Arguments (@(
        $ProjectPath,
        "/nologo",
        "/verbosity:quiet"
    ) + $buildProperties + @("/t:Build"))

    if ($result.ExitCode -ne 0) {
        Write-Host ($result.Output | Out-String)
        throw "$ProjectLabel build failed."
    }
}

function Invoke-InstallerExecutableBuild {
    param(
        [hashtable]$Builder,
        [object]$Projects
    )

    Invoke-ProjectBuild `
        -Builder $Builder `
        -ProjectPath $Projects.InstallerProjectPath `
        -ProjectLabel "installer executable" `
        -ExtraBuildProperties @("/p:InstallerBuild=true", "/p:BuildProjectReferences=false")
}

function Test-ArtifactOutputAvailable {
    param(
        [string]$ArtifactName,
        [System.Collections.Generic.List[string]]$Errors
    )

    if (Clear-OutputFile $ArtifactName) {
        return $true
    }

    [void]$Errors.Add("$ArtifactName is locked by another process.")
    return $false
}

function New-PortableArtifacts {
    param(
        [object]$Projects,
        [System.Collections.Generic.List[string]]$Errors
    )

    $portableArtifacts = @(
        @{
            BuildLabel         = "net45"
            TargetSubdirectory = "net45"
            ArtifactName       = $script:StandardArtifactName
        },
        @{
            BuildLabel         = "net30"
            TargetSubdirectory = "net30"
            ArtifactName       = $script:LegacyArtifactName
        }
    )

    foreach ($artifact in $portableArtifacts) {
        if (-not (Test-ArtifactOutputAvailable -ArtifactName $artifact.ArtifactName -Errors $Errors)) {
            continue
        }

        try {
            $script:OverlayStats[$artifact.ArtifactName] = New-OverlayExe `
                -BuildLabel $artifact.BuildLabel `
                -Configuration $Configuration `
                -ProjectRoot $Projects.StandaloneProjectRoot `
                -TargetSubdirectory $artifact.TargetSubdirectory `
                -ArtifactName $artifact.ArtifactName
        }
        catch {
            [void]$Errors.Add("$($artifact.ArtifactName) : $_")
        }
    }
}

function New-InstallerArtifacts {
    param(
        [hashtable]$Builder,
        [object]$Projects,
        [System.Collections.Generic.List[string]]$Errors
    )

    $nsisOutputAvailable = if (-not $SkipNSIS) {
        Test-ArtifactOutputAvailable -ArtifactName $script:FullInstallerArtifactName -Errors $Errors
    }
    else {
        Write-Warning "NSIS installer build skipped (-SkipNSIS)"
        $false
    }

    $msiOutputAvailable = if (-not $SkipMSI) {
        Test-ArtifactOutputAvailable -ArtifactName $script:MSIArtifactName -Errors $Errors
    }
    else {
        Write-Warning "MSI build skipped (-SkipMSI)"
        $false
    }

    if (-not ($nsisOutputAvailable -or $msiOutputAvailable)) {
        return
    }

    try {
        Invoke-InstallerExecutableBuild -Builder $Builder -Projects $Projects
    }
    catch {
        $buildError = $_.Exception.Message
        if ($buildError -ieq "installer executable build failed.") {
            $buildError = "See MSBuild output above."
        }

        [void]$Errors.Add("Installer executable build failed. $buildError")
        return
    }

    if ($nsisOutputAvailable) {
        try {
            New-NSISInstaller `
                -Configuration $Configuration `
                -ProjectRoot $Projects.InstallerProjectRoot `
                -ArtifactName $script:FullInstallerArtifactName |
                Out-Null
        }
        catch {
            [void]$Errors.Add("$($script:FullInstallerArtifactName) : $_")
        }
    }

    if ($msiOutputAvailable) {
        $msiModule = Join-Path $script:PublishRoot "modules\msi.psm1"
        try {
            Import-Module $msiModule -Force
            New-MSIInstaller `
                -Configuration $Configuration `
                -ProjectRoot $Projects.InstallerProjectRoot `
                -ArtifactName $script:MSIArtifactName
        }
        catch {
            [void]$Errors.Add("$($script:MSIArtifactName) : $_")
        }
    }
}

function Write-BuiltArtifactSummary {
    Write-Step "Build completed at $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"

    $expectedArtifacts = [System.Collections.Generic.List[string]]::new()
    [void]$expectedArtifacts.Add($script:StandardArtifactName)
    [void]$expectedArtifacts.Add($script:LegacyArtifactName)
    if (-not $SkipNSIS) {
        [void]$expectedArtifacts.Add($script:FullInstallerArtifactName)
    }
    if (-not $SkipMSI) {
        [void]$expectedArtifacts.Add($script:MSIArtifactName)
    }

    foreach ($artifact in $expectedArtifacts) {
        $artifactPath = Join-Path $script:PublishOutputPath $artifact
        if (Test-Path -LiteralPath $artifactPath) {
            $size = [math]::Round((Get-Item -LiteralPath $artifactPath).Length / 1KB, 1)
            $ratioSuffix = ""
            if ($script:OverlayStats.ContainsKey($artifact) -and $null -ne $script:OverlayStats[$artifact]) {
                $ratio = $script:OverlayStats[$artifact].OverallRatio
                $ratioSuffix = ", ${ratio}% of original"
            }
            Write-Host "  $artifact ($size KB${ratioSuffix})"
        }
    }
}

Write-Step "Resolving build inputs"
$projects = Resolve-BuildProjects

Write-Step "Checking build prerequisites"
$builder = Get-MSBuildCommand
Assert-BuildPrerequisites -Projects $projects

Invoke-ProjectRestore -Builder $builder -ProjectPath $projects.StandaloneProjectPath -ProjectLabel "standalone project"
Invoke-ProjectRestore `
    -Builder $builder `
    -ProjectPath $projects.InstallerProjectPath `
    -ProjectLabel "installer project" `
    -ExtraRestoreProperties @("/p:InstallerBuild=true")

Write-Step "Preparing the publish output directory"
Clear-PublishStagingDirectory
Clear-BuildOutputDirectories
New-Item -ItemType Directory -Path $script:PublishOutputPath -Force | Out-Null
New-Item -ItemType Directory -Path $script:PublishStagingPath -Force | Out-Null

Invoke-ProjectBuild -Builder $builder -ProjectPath $projects.StandaloneProjectPath -ProjectLabel "standalone executable"

if ($BuildOnly) {
    Invoke-InstallerExecutableBuild -Builder $builder -Projects $projects
    return
}

try {
    $packagingErrors = [System.Collections.Generic.List[string]]::new()

    New-PortableArtifacts -Projects $projects -Errors $packagingErrors
    New-InstallerArtifacts -Builder $builder -Projects $projects -Errors $packagingErrors
    Write-BuiltArtifactSummary

    if ($packagingErrors.Count -gt 0) {
        $message = "One or more packaging steps failed:`n" + ($packagingErrors -join "`n")
        throw $message
    }
}
finally {
    Clear-PublishStagingDirectory
}
