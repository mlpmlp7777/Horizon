[CmdletBinding()]
param(
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version = '0.1.0',

    [ValidateSet('win-x64')]
    [string]$Runtime = 'win-x64',

    [switch]$SkipTests
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$projectRoot = Split-Path -Parent $PSScriptRoot
$artifactsRoot = Join-Path $projectRoot 'artifacts'
$publishDir = Join-Path $artifactsRoot "publish\$Runtime"
$installerDir = Join-Path $artifactsRoot 'installer'
$appProject = Join-Path $projectRoot 'src\Horizon.App\Horizon.App.csproj'
$testProject = Join-Path $projectRoot 'tests\Horizon.App.Tests\Horizon.App.Tests.csproj'
$installerScript = Join-Path $projectRoot 'installer\Horizon.iss'

$isccCandidates = @(
    (Join-Path $env:LOCALAPPDATA 'Programs\Inno Setup 6\ISCC.exe'),
    'C:\Program Files (x86)\Inno Setup 6\ISCC.exe',
    'C:\Program Files\Inno Setup 6\ISCC.exe'
)
$iscc = $isccCandidates | Where-Object { Test-Path -LiteralPath $_ } | Select-Object -First 1
if (-not $iscc) {
    throw 'Inno Setup 6 was not found. Install it with: winget install --id JRSoftware.InnoSetup -e'
}

if (-not $SkipTests) {
    & dotnet run --project $testProject -c Release
    if ($LASTEXITCODE -ne 0) {
        throw "Tests failed with exit code $LASTEXITCODE"
    }
}

$resolvedArtifactsRoot = [System.IO.Path]::GetFullPath($artifactsRoot).TrimEnd('\') + '\'
$resolvedPublishDir = [System.IO.Path]::GetFullPath($publishDir)
if (-not $resolvedPublishDir.StartsWith($resolvedArtifactsRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Publish directory is outside artifacts; refusing to clean: $resolvedPublishDir"
}

if (Test-Path -LiteralPath $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}
New-Item -ItemType Directory -Path $publishDir -Force | Out-Null
New-Item -ItemType Directory -Path $installerDir -Force | Out-Null

& dotnet publish $appProject `
    -c Release `
    -r $Runtime `
    --self-contained true `
    -o $publishDir `
    -p:Version=$Version `
    -p:DebugType=None `
    -p:DebugSymbols=false `
    -p:PublishTrimmed=false
if ($LASTEXITCODE -ne 0) {
    throw "Publish failed with exit code $LASTEXITCODE"
}

& $iscc `
    "/DMyAppVersion=$Version" `
    "/DSourceDir=$publishDir" `
    "/DOutputDir=$installerDir" `
    $installerScript
if ($LASTEXITCODE -ne 0) {
    throw "Installer compilation failed with exit code $LASTEXITCODE"
}

$setupPath = Join-Path $installerDir "Horizon-Setup-v$Version-x64.exe"
if (-not (Test-Path -LiteralPath $setupPath)) {
    throw "Installer output was not found: $setupPath"
}

$file = Get-Item -LiteralPath $setupPath
$hash = Get-FileHash -LiteralPath $setupPath -Algorithm SHA256
Write-Host ''
Write-Host 'Horizon installer created:'
Write-Host "  Path: $($file.FullName)"
Write-Host "  Size: $([Math]::Round($file.Length / 1MB, 2)) MB"
Write-Host "  SHA256: $($hash.Hash)"
