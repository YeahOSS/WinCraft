Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

Import-Module (Join-Path $PSScriptRoot "common.psm1")

$script:RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$script:SourceRoot = Join-Path $script:RepoRoot "src"
$script:OutputPath = Join-Path (Split-Path -Parent $PSScriptRoot) "output"

function Initialize-LzmaSdk {
    if ($null -ne ("SevenZip.Compression.LZMA.Encoder" -as [type])) {
        return
    }

    $sdkPath = Join-Path $script:SourceRoot "third_party\LzmaSdk"
    Assert-PathExists -Path $sdkPath -Description "LZMA SDK source directory"

    $sourceFiles = @(
        (Join-Path $sdkPath "ICoder.cs"),
        (Join-Path $sdkPath "Common\CRC.cs"),
        (Join-Path $sdkPath "Compress\LZ\IMatchFinder.cs"),
        (Join-Path $sdkPath "Compress\LZ\LzBinTree.cs"),
        (Join-Path $sdkPath "Compress\LZ\LzInWindow.cs"),
        (Join-Path $sdkPath "Compress\LZ\LzOutWindow.cs"),
        (Join-Path $sdkPath "Compress\LZMA\LzmaBase.cs"),
        (Join-Path $sdkPath "Compress\LZMA\LzmaEncoder.cs"),
        (Join-Path $sdkPath "Compress\RangeCoder\RangeCoder.cs"),
        (Join-Path $sdkPath "Compress\RangeCoder\RangeCoderBit.cs"),
        (Join-Path $sdkPath "Compress\RangeCoder\RangeCoderBitTree.cs")
    )
    foreach ($sourceFile in $sourceFiles) {
        Assert-PathExists -Path $sourceFile -Description "LZMA SDK source file"
    }

    Add-Type -Path $sourceFiles
}

function Compress-Lzma {
    param(
        [byte[]]$RawBytes
    )

    Initialize-LzmaSdk

    $encoder = New-Object SevenZip.Compression.LZMA.Encoder
    $propIDs = [SevenZip.CoderPropID[]]@(
        [SevenZip.CoderPropID]::DictionarySize,
        [SevenZip.CoderPropID]::PosStateBits,
        [SevenZip.CoderPropID]::LitContextBits,
        [SevenZip.CoderPropID]::LitPosBits,
        [SevenZip.CoderPropID]::Algorithm,
        [SevenZip.CoderPropID]::NumFastBytes,
        [SevenZip.CoderPropID]::MatchFinder,
        [SevenZip.CoderPropID]::EndMarker
    )
    $properties = [object[]]@(
        [int](1 -shl 23),
        [int]2,
        [int]3,
        [int]0,
        [int]2,
        [int]128,
        "bt4",
        $false
    )

    $encoder.SetCoderProperties($propIDs, $properties)

    $inputStream = New-Object System.IO.MemoryStream -ArgumentList (,$RawBytes)
    $compressedStream = New-Object System.IO.MemoryStream
    $propertiesStream = New-Object System.IO.MemoryStream

    try {
        $encoder.WriteCoderProperties($propertiesStream)
        $encoder.Code($inputStream, $compressedStream, [int64]-1, [int64]-1, $null)

        return [pscustomobject]@{
            CompressedBytes = $compressedStream.ToArray()
            Properties = $propertiesStream.ToArray()
        }
    }
    finally {
        $propertiesStream.Dispose()
        $compressedStream.Dispose()
        $inputStream.Dispose()
    }
}

# Builds a single-file executable by appending a compressed container of
# dependency DLLs as a PE overlay.  At runtime OverlayAssemblyResolver reads
# the overlay, decompresses the container, and serves assemblies from memory.
function New-OverlayExe {
    param(
        [string]$BuildLabel,
        [string]$Configuration,
        [string]$ProjectRoot,
        [string]$TargetSubdirectory,
        [string]$ArtifactName
    )

    Write-Step "Building $BuildLabel single-file executable with dependency overlay"

    $buildOutputDirectory = Join-Path $ProjectRoot "bin\$Configuration\$TargetSubdirectory"
    $exePath = Join-Path $buildOutputDirectory "WinCraft.exe"
    $artifactPath = Join-Path $script:OutputPath $ArtifactName

    Assert-PathExists -Path $exePath -Description "$BuildLabel executable"

    # Collect all .dll files in the build output.
    $dllPaths = @(Get-ChildItem -Path $buildOutputDirectory -Filter "*.dll" -File | Select-Object -ExpandProperty FullName)

    # Build a flat container: [int32 count] (for each: [int16 nameLen] [name] [int32 dataLen] [data]).
    $containerStream = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter($containerStream, [System.Text.Encoding]::UTF8)
    $writer.Write([int32]$dllPaths.Count)
    foreach ($dllPath in $dllPaths) {
        $name = Split-Path $dllPath -Leaf
        $nameBytes = [System.Text.Encoding]::UTF8.GetBytes($name)
        $data = [System.IO.File]::ReadAllBytes($dllPath)
        $writer.Write([int16]$nameBytes.Length)
        $writer.Write($nameBytes)
        $writer.Write([int32]$data.Length)
        $writer.Write($data)
    }
    $writer.Flush()
    $rawBytes = $containerStream.ToArray()
    $writer.Dispose()
    $containerStream.Dispose()

    $lzmaResult = Compress-Lzma -RawBytes $rawBytes
    $compressedBytes = $lzmaResult.CompressedBytes
    $lzmaProperties = $lzmaResult.Properties
    $dllCount = $dllPaths.Count
    $ratio = [math]::Round($compressedBytes.Length / [math]::Max(1, $rawBytes.Length) * 100, 1)
    Write-Host "==> Overlay: $dllCount DLL(s), $($rawBytes.Length) -> $($compressedBytes.Length) bytes (${ratio}%)"

    # Append overlay: [compressed data] [5 bytes LZMA props] [8 bytes raw len] [4 bytes compressed len] [4 bytes magic "WOLZ"]
    $magic = [System.BitConverter]::GetBytes([uint32]0x5A4C4F57)
    $rawLenBytes = [System.BitConverter]::GetBytes([int64]$rawBytes.Length)
    $lenBytes = [System.BitConverter]::GetBytes([int32]$compressedBytes.Length)

    if (Test-Path -LiteralPath $artifactPath) {
        Remove-Item -LiteralPath $artifactPath -Force
    }

    Copy-Item -LiteralPath $exePath -Destination $artifactPath -Force

    $exeBytes = [System.IO.File]::ReadAllBytes($artifactPath)
    $footerSize = 21
    $overlayOffset = $exeBytes.Length
    [Array]::Resize([ref]$exeBytes, $overlayOffset + $compressedBytes.Length + $footerSize)
    [Array]::Copy($compressedBytes, 0, $exeBytes, $overlayOffset, $compressedBytes.Length)
    [Array]::Copy($lzmaProperties, 0, $exeBytes, $overlayOffset + $compressedBytes.Length, 5)
    [Array]::Copy($rawLenBytes, 0, $exeBytes, $overlayOffset + $compressedBytes.Length + 5, 8)
    [Array]::Copy($lenBytes, 0, $exeBytes, $overlayOffset + $compressedBytes.Length + 13, 4)
    [Array]::Copy($magic, 0, $exeBytes, $overlayOffset + $compressedBytes.Length + 17, 4)
    [System.IO.File]::WriteAllBytes($artifactPath, $exeBytes)

    Assert-PathExists -Path $artifactPath -Description "$BuildLabel single-file artifact"
}

Export-ModuleMember -Function New-OverlayExe
