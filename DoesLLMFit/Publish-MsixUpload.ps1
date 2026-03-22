<#
.SYNOPSIS
    Builds and packages the DoesLLMFit app as an .msixupload for Microsoft Store submission.
.DESCRIPTION
    The "Package and Publish" / "Create App Packages" wizard is a Visual Studio feature
    not available in VS Code. This script replicates that workflow from the CLI:
      1. Publishes the WinAppSDK project as a signed MSIX
      2. Zips the .msix into a .msixupload (the format Partner Center accepts)
.PARAMETER CertThumbprint
    Thumbprint of the code-signing certificate in Cert:\CurrentUser\My.
    If omitted, falls back to the value in the publish profile.
.PARAMETER Platform
    Target platform: x64 (default), x86, or arm64.
.PARAMETER Configuration
    Build configuration: Release (default) or Debug.
#>
param(
    [string]$CertThumbprint,
    [string]$Platform = "x64",
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"
$ProjectDir = $PSScriptRoot
$ProjectFile = Join-Path $ProjectDir "DoesLLMFit.csproj"
$TFM = "net9.0-windows10.0.26100"

# Build args
$buildArgs = @(
    "publish", $ProjectFile,
    "-f", $TFM,
    "-c", $Configuration,
    "-p:Platform=$Platform",
    "-p:GenerateAppxPackageOnBuild=true",
    "-p:AppxBundle=Never",
    "-p:PublishTrimmed=false",
    "-p:PublishReadyToRun=false",
    "-p:AppxPackageSigningEnabled=true"
)

if ($CertThumbprint) {
    $buildArgs += "-p:PackageCertificateThumbprint=$CertThumbprint"
}

Write-Host "Building MSIX package..." -ForegroundColor Cyan
dotnet @buildArgs
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }

# Find the generated .msix
$appPackagesDir = Join-Path $ProjectDir "bin\$Platform\$Configuration\$TFM\win-$Platform\AppPackages"
$msixFile = Get-ChildItem -Path $appPackagesDir -Recurse -Filter "*.msix" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1

if (-not $msixFile) { throw "No .msix file found in $appPackagesDir" }

Write-Host "Found MSIX: $($msixFile.FullName)" -ForegroundColor Green

# Create .msixupload (zip containing the .msix, renamed to .msixupload)
$uploadFileName = [System.IO.Path]::ChangeExtension($msixFile.Name, ".msixupload")
$uploadPath = Join-Path $msixFile.DirectoryName $uploadFileName
$zipPath = [System.IO.Path]::ChangeExtension($uploadPath, ".zip")

if (Test-Path $uploadPath) { Remove-Item $uploadPath -Force }
if (Test-Path $zipPath) { Remove-Item $zipPath -Force }

Compress-Archive -Path $msixFile.FullName -DestinationPath $zipPath -CompressionLevel Optimal
Rename-Item $zipPath $uploadPath

Write-Host ""
Write-Host "=== MSIX Upload Package Ready ===" -ForegroundColor Green
Write-Host "  MSIX:       $($msixFile.FullName)" -ForegroundColor White
Write-Host "  Upload:     $uploadPath" -ForegroundColor White
Write-Host "  Size:       $([math]::Round((Get-Item $uploadPath).Length / 1MB, 2)) MB" -ForegroundColor White
Write-Host ""
Write-Host "Submit '$uploadFileName' to Microsoft Partner Center." -ForegroundColor Yellow
