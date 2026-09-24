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
Compress-Archive -Path (Join-Path $stagingRoot "*") -DestinationPath $archivePath -CompressionLevel Optimal
Write-Output $archivePath
