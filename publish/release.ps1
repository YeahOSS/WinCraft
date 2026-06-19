[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

# Decode native-command output (git) as UTF-8 to avoid garbled characters
# when the system console code page differs from the tool output encoding.
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

$script:PublishRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$script:RepositoryRoot = Split-Path -Parent $script:PublishRoot
$script:VersionPropsPath = Join-Path $script:RepositoryRoot "src\Version.props"
$script:BuildScriptPath = Join-Path $script:PublishRoot "build.ps1"

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

function Assert-CommandExists {
    param(
        [string]$CommandName
    )

    $command = Get-Command $CommandName -ErrorAction SilentlyContinue

    if ($null -eq $command) {
        throw "Required command was not found: $CommandName"
    }
}

function Assert-GitIdentityConfigured {
    $userName = (& git config --get user.name)

    if ($LASTEXITCODE -ne 0) {
        $userName = $null
    }

    $userEmail = (& git config --get user.email)

    if ($LASTEXITCODE -ne 0) {
        $userEmail = $null
    }

    if ([string]::IsNullOrWhiteSpace($userName) -or [string]::IsNullOrWhiteSpace($userEmail)) {
        throw "Git user.name and user.email must be configured before running release.ps1."
    }
}

function Test-GitWorkingTreeClean {
    $statusOutput = & git status --short

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to read the git working tree state."
    }

    return [string]::IsNullOrWhiteSpace(($statusOutput | Out-String))
}

function Get-VersionParts {
    param(
        [string]$Value
    )

    if ($Value -notmatch '^(?<major>\d+)\.(?<minor>\d+)\.(?<build>\d+)$') {
        throw "Version must use the format major.minor.build."
    }

    return @{
        Major = $Matches.major
        Minor = $Matches.minor
        Build = $Matches.build
    }
}

function Get-CurrentVersion {
    Assert-PathExists -Path $script:VersionPropsPath -Description "Version props file"

    [xml]$versionDocument = Get-Content -LiteralPath $script:VersionPropsPath
    $propertyGroup = $versionDocument.Project.PropertyGroup

    if ($null -eq $propertyGroup) {
        throw "Version.props does not contain a PropertyGroup."
    }

    $major = [string]$propertyGroup.VersionMajor
    $minor = [string]$propertyGroup.VersionMinor
    $build = [string]$propertyGroup.VersionBuild

    if ([string]::IsNullOrWhiteSpace($major) -or [string]::IsNullOrWhiteSpace($minor) -or [string]::IsNullOrWhiteSpace($build)) {
        throw "Version.props must define VersionMajor, VersionMinor, and VersionBuild."
    }

    return "$major.$minor.$build"
}

function Set-VersionProps {
    param(
        [hashtable]$VersionParts
    )

    $content = [System.IO.File]::ReadAllText($script:VersionPropsPath)

    $content = [System.Text.RegularExpressions.Regex]::Replace(
        $content,
        '<VersionMajor>\d+</VersionMajor>',
        "<VersionMajor>$($VersionParts.Major)</VersionMajor>")

    $content = [System.Text.RegularExpressions.Regex]::Replace(
        $content,
        '<VersionMinor>\d+</VersionMinor>',
        "<VersionMinor>$($VersionParts.Minor)</VersionMinor>")

    $content = [System.Text.RegularExpressions.Regex]::Replace(
        $content,
        '<VersionBuild>\d+</VersionBuild>',
        "<VersionBuild>$($VersionParts.Build)</VersionBuild>")

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($script:VersionPropsPath, $content, $utf8NoBom)
}

function Assert-TagDoesNotExist {
    param(
        [string]$TagName
    )

    $tagValue = & git tag --list $TagName

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to query existing git tags."
    }

    if ($tagValue -contains $TagName) {
        throw "Git tag already exists: $TagName"
    }
}

function Invoke-BuildScript {
    Assert-PathExists -Path $script:BuildScriptPath -Description "Build script"

    & $script:BuildScriptPath

    if ($LASTEXITCODE -ne 0) {
        throw "Publish build failed."
    }
}

Assert-CommandExists -CommandName "git"
Assert-GitIdentityConfigured
Assert-PathExists -Path $script:VersionPropsPath -Description "Version props file"
Assert-PathExists -Path $script:BuildScriptPath -Description "Build script"

$targetVersionParts = Get-VersionParts -Value $Version
$currentVersion = Get-CurrentVersion
$targetVersion = "$($targetVersionParts.Major).$($targetVersionParts.Minor).$($targetVersionParts.Build)"
$tagName = "v$targetVersion"

if ($currentVersion -eq $targetVersion) {
    throw "The requested version matches the current version: $targetVersion"
}

if (-not (Test-GitWorkingTreeClean)) {
    throw "The git working tree must be clean before running release.ps1."
}

Assert-TagDoesNotExist -TagName $tagName

$originalVersionPropsContent = [System.IO.File]::ReadAllText($script:VersionPropsPath)
$commitCreated = $false

try {
    Write-Step "Updating version metadata"
    Set-VersionProps -VersionParts $targetVersionParts

    Write-Step "Building release artifacts"
    Invoke-BuildScript

    Write-Step "Creating the release commit"
    & git add -- $script:VersionPropsPath

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to stage Version.props."
    }

    & git commit -m "release: bump version to $targetVersion"

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create the release commit."
    }

    $commitCreated = $true

    Write-Step "Creating the release tag"
    & git tag $tagName

    if ($LASTEXITCODE -ne 0) {
        throw "Failed to create the git tag."
    }
}
catch {
    if (-not $commitCreated) {
        $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
        [System.IO.File]::WriteAllText($script:VersionPropsPath, $originalVersionPropsContent, $utf8NoBom)
    }
    else {
        Write-Host ""
        Write-Host "Release commit was created before the failure occurred."
        Write-Host "Create the missing tag manually or revert the release commit before retrying."
    }

    throw
}

Write-Step "Release completed"
Write-Host "Version: $targetVersion"
Write-Host "Tag: $tagName"
Write-Host "Next: push the release commit and tag to the remote repository."
