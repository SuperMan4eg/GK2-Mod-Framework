param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$projectRoot = $PSScriptRoot
$artifactsRoot = Join-Path $projectRoot "Artifacts"
$buildProps = [xml](Get-Content -Raw -LiteralPath (Join-Path $projectRoot "Directory.Build.props"))
$releaseVersion = [string]$buildProps.Project.PropertyGroup.Version
if ([string]::IsNullOrWhiteSpace($releaseVersion)) {
    throw "Release blocked: Directory.Build.props does not define Version."
}
$stagingRoot = Join-Path $artifactsRoot "GK2-Mod-Framework-$releaseVersion"
$archivePath = Join-Path $artifactsRoot "GK2-Mod-Framework-$releaseVersion.zip"
$licensePath = Join-Path $projectRoot "LICENSE"

function Get-ZipCentralDirectoryInfo {
    param([Parameter(Mandatory = $true)][byte[]]$Bytes)

    $minimumEocdSize = 22
    $maximumCommentSize = 65535
    $searchStart = $Bytes.Length - $minimumEocdSize
    $searchEnd = [Math]::Max(0, $Bytes.Length - $minimumEocdSize - $maximumCommentSize)
    $eocdOffset = -1

    for ($i = $searchStart; $i -ge $searchEnd; $i--) {
        $isEocd = $Bytes[$i] -eq 0x50 -and $Bytes[$i + 1] -eq 0x4B -and $Bytes[$i + 2] -eq 0x05 -and $Bytes[$i + 3] -eq 0x06
        if ($isEocd) {
            $eocdOffset = $i
            break
        }
    }

    if ($eocdOffset -lt 0) {
        throw "Portable ZIP validation failed: EOCD record was not found."
    }

    $entryCount = [BitConverter]::ToUInt16($Bytes, $eocdOffset + 10)
    $centralOffset = [int][BitConverter]::ToUInt32($Bytes, $eocdOffset + 16)

    return [pscustomobject]@{
        EntryCount = [int]$entryCount
        CentralOffset = $centralOffset
    }
}

function Set-And-Test-PortableZipPermissions {
    param([Parameter(Mandatory = $true)][string]$Path)

    $bytes = [IO.File]::ReadAllBytes($Path)
    $central = Get-ZipCentralDirectoryInfo -Bytes $bytes
    $position = $central.CentralOffset

    for ($index = 0; $index -lt $central.EntryCount; $index++) {
        $validSignature = $bytes[$position] -eq 0x50 -and $bytes[$position + 1] -eq 0x4B -and $bytes[$position + 2] -eq 0x01 -and $bytes[$position + 3] -eq 0x02
        if (-not $validSignature) {
            throw "Portable ZIP validation failed: central directory entry $index has an invalid signature."
        }

        $nameLength = [int][BitConverter]::ToUInt16($bytes, $position + 28)
        $extraLength = [int][BitConverter]::ToUInt16($bytes, $position + 30)
        $commentLength = [int][BitConverter]::ToUInt16($bytes, $position + 32)
        $name = [Text.Encoding]::UTF8.GetString($bytes, $position + 46, $nameLength)
        $isDirectory = $name.EndsWith("/", [StringComparison]::Ordinal)

        # ZIP "version made by" high byte: 3 = Unix.
        $bytes[$position + 5] = 3

        # Unix file type + mode in the high 16 bits. Keep the DOS directory bit for folders.
        [uint32]$unixMode = if ($isDirectory) { 0x41ED } else { 0x81A4 } # 040755 / 0100644
        [uint32]$externalAttributes = $unixMode -shl 16
        if ($isDirectory) { $externalAttributes = $externalAttributes -bor 0x10 }
        [BitConverter]::GetBytes($externalAttributes).CopyTo($bytes, $position + 38)

        $position += 46 + $nameLength + $extraLength + $commentLength
    }

    [IO.File]::WriteAllBytes($Path, $bytes)

    # Re-read exactly what will be published and fail the build if metadata is not portable.
    $verifiedBytes = [IO.File]::ReadAllBytes($Path)
    $verifiedCentral = Get-ZipCentralDirectoryInfo -Bytes $verifiedBytes
    $position = $verifiedCentral.CentralOffset

    for ($index = 0; $index -lt $verifiedCentral.EntryCount; $index++) {
        $nameLength = [int][BitConverter]::ToUInt16($verifiedBytes, $position + 28)
        $extraLength = [int][BitConverter]::ToUInt16($verifiedBytes, $position + 30)
        $commentLength = [int][BitConverter]::ToUInt16($verifiedBytes, $position + 32)
        $name = [Text.Encoding]::UTF8.GetString($verifiedBytes, $position + 46, $nameLength)
        $isDirectory = $name.EndsWith("/", [StringComparison]::Ordinal)
        $madeByPlatform = [int]$verifiedBytes[$position + 5]
        [uint32]$externalAttributes = [BitConverter]::ToUInt32($verifiedBytes, $position + 38)
        [uint32]$mode = ($externalAttributes -shr 16) -band 0xFFFF
        [uint32]$expectedMode = if ($isDirectory) { 0x41ED } else { 0x81A4 }

        if ($madeByPlatform -ne 3 -or $mode -ne $expectedMode) {
            throw "Portable ZIP validation failed for '$name': platform=$madeByPlatform mode=0x$($mode.ToString('X4')); expected Unix mode=0x$($expectedMode.ToString('X4'))."
        }

        $position += 46 + $nameLength + $extraLength + $commentLength
    }
}

if (-not (Test-Path -LiteralPath $licensePath -PathType Leaf)) {
    throw "Release blocked: choose and add LICENSE before publishing."
}

dotnet build (Join-Path $projectRoot "src\GK2.Framework\GK2.Framework.csproj") -c $Configuration
if ($LASTEXITCODE -ne 0) { throw "Framework build failed." }

if (Test-Path -LiteralPath $stagingRoot) {
    $resolvedArtifacts = [System.IO.Path]::GetFullPath($artifactsRoot)
    $resolvedStaging = [System.IO.Path]::GetFullPath($stagingRoot)
    if (-not $resolvedStaging.StartsWith($resolvedArtifacts + [System.IO.Path]::DirectorySeparatorChar)) {
        throw "Unsafe staging path: $resolvedStaging"
    }
    Remove-Item -LiteralPath $resolvedStaging -Recurse -Force
}

New-Item -ItemType Directory -Path (Join-Path $stagingRoot "BepInEx\plugins") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $stagingRoot "BepInEx\plugins\GK2.Framework\Localization") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $stagingRoot "docs") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $stagingRoot "Templates\GK2.Framework.ModTemplate") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $stagingRoot "Templates\GK2.Framework.OptionalIntegrationTemplate\Main") -Force | Out-Null
New-Item -ItemType Directory -Path (Join-Path $stagingRoot "Templates\GK2.Framework.OptionalIntegrationTemplate\FrameworkBridge") -Force | Out-Null

Copy-Item -LiteralPath (Join-Path $projectRoot "src\GK2.Framework\bin\$Configuration\netstandard2.1\GK2.Framework.dll") -Destination (Join-Path $stagingRoot "BepInEx\plugins\GK2.Framework.dll")
Copy-Item -Path (Join-Path $projectRoot "Localization\*") -Destination (Join-Path $stagingRoot "BepInEx\plugins\GK2.Framework\Localization") -Recurse
Copy-Item -LiteralPath (Join-Path $projectRoot "README.md") -Destination $stagingRoot
Copy-Item -LiteralPath (Join-Path $projectRoot "CHANGELOG.md") -Destination $stagingRoot
Copy-Item -LiteralPath $licensePath -Destination $stagingRoot
Copy-Item -LiteralPath (Join-Path $projectRoot "docs\NEW_MOD_GUIDE.md") -Destination (Join-Path $stagingRoot "docs")
Copy-Item -LiteralPath (Join-Path $projectRoot "docs\PUBLIC_API.md") -Destination (Join-Path $stagingRoot "docs")
Copy-Item -LiteralPath (Join-Path $projectRoot "docs\OPTIONAL_INTEGRATION.md") -Destination (Join-Path $stagingRoot "docs")
Copy-Item -LiteralPath (Join-Path $projectRoot "Templates\GK2.Framework.ModTemplate\GK2.Framework.ModTemplate.csproj") -Destination (Join-Path $stagingRoot "Templates\GK2.Framework.ModTemplate")
Copy-Item -LiteralPath (Join-Path $projectRoot "Templates\GK2.Framework.ModTemplate\Plugin.cs") -Destination (Join-Path $stagingRoot "Templates\GK2.Framework.ModTemplate")
Copy-Item -LiteralPath (Join-Path $projectRoot "Templates\GK2.Framework.ModTemplate\README.md") -Destination (Join-Path $stagingRoot "Templates\GK2.Framework.ModTemplate")
Copy-Item -LiteralPath (Join-Path $projectRoot "Templates\GK2.Framework.OptionalIntegrationTemplate\README.md") -Destination (Join-Path $stagingRoot "Templates\GK2.Framework.OptionalIntegrationTemplate")
Copy-Item -LiteralPath (Join-Path $projectRoot "Templates\GK2.Framework.OptionalIntegrationTemplate\Main\Main.csproj") -Destination (Join-Path $stagingRoot "Templates\GK2.Framework.OptionalIntegrationTemplate\Main")
Copy-Item -LiteralPath (Join-Path $projectRoot "Templates\GK2.Framework.OptionalIntegrationTemplate\Main\MainPlugin.cs") -Destination (Join-Path $stagingRoot "Templates\GK2.Framework.OptionalIntegrationTemplate\Main")
Copy-Item -LiteralPath (Join-Path $projectRoot "Templates\GK2.Framework.OptionalIntegrationTemplate\FrameworkBridge\FrameworkBridge.csproj") -Destination (Join-Path $stagingRoot "Templates\GK2.Framework.OptionalIntegrationTemplate\FrameworkBridge")
Copy-Item -LiteralPath (Join-Path $projectRoot "Templates\GK2.Framework.OptionalIntegrationTemplate\FrameworkBridge\FrameworkBridgePlugin.cs") -Destination (Join-Path $stagingRoot "Templates\GK2.Framework.OptionalIntegrationTemplate\FrameworkBridge")

$expected = Get-Content -LiteralPath (Join-Path $projectRoot "release-manifest.txt") |
    Where-Object { $_ -and -not $_.TrimStart().StartsWith("#") } |
    ForEach-Object { $_.Trim().Replace("\", "/") } |
    Sort-Object
$actual = Get-ChildItem -LiteralPath $stagingRoot -Recurse -File |
    ForEach-Object { $_.FullName.Substring($stagingRoot.Length + 1).Replace("\", "/") } |
    Sort-Object
$difference = Compare-Object -ReferenceObject $expected -DifferenceObject $actual
if ($difference) {
    throw "Release staging does not match release-manifest.txt:`n$($difference | Out-String)"
}

$publicTextFiles = Get-ChildItem -LiteralPath $stagingRoot -Recurse -File |
    Where-Object { $_.Extension -in ".md", ".txt", ".cs", ".csproj" }
foreach ($publicTextFile in $publicTextFiles) {
    $publicText = Get-Content -Raw -LiteralPath $publicTextFile.FullName -Encoding UTF8
    if ($publicText -match "[\u0400-\u04FF]") {
        $relativePath = $publicTextFile.FullName.Substring($stagingRoot.Length + 1)
        throw "Release blocked: public text contains Cyrillic characters: $relativePath"
    }
}

if (Test-Path -LiteralPath $archivePath) { Remove-Item -LiteralPath $archivePath -Force }

$tar = Get-Command tar.exe -ErrorAction SilentlyContinue
if ($tar -eq $null) {
    throw "Release blocked: Windows tar.exe/libarchive is required to create a portable ZIP."
}

$topLevelEntries = Get-ChildItem -LiteralPath $stagingRoot |
    Sort-Object Name |
    ForEach-Object { $_.Name }
if ($topLevelEntries.Count -eq 0) {
    throw "Release blocked: staging directory is empty."
}

& $tar.Source -a -c -f $archivePath -C $stagingRoot @topLevelEntries
if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $archivePath -PathType Leaf)) {
    throw "Release blocked: tar.exe failed to create the release ZIP."
}

Set-And-Test-PortableZipPermissions -Path $archivePath
Write-Output "PORTABLE_ZIP_PERMISSIONS_PASS: unix directories=0755; files=0644"
Write-Output $archivePath
