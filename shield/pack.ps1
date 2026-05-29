# Zenith Anti-PMO - Unified Compile and Pack Script
# Compiles all C# executables and packages the entire suite into the downloadable ZIP archive for the website.

$rootDir = Resolve-Path "$PSScriptRoot\.."
$docsDir = Join-Path $rootDir "docs"
$zipPath = Join-Path $docsDir "downloads\zenith-anti-pmo.zip"

Write-Host "1. Running C# Compilation..." -ForegroundColor Cyan
Set-Location $PSScriptRoot
.\compile.ps1

Write-Host "`n2. Packaging ZIP archive for distribution..." -ForegroundColor Cyan

# Ensure the destination directory exists
$downloadsDir = Split-Path $zipPath
if (-not (Test-Path $downloadsDir)) {
    New-Item -ItemType Directory -Path $downloadsDir -Force | Out-Null
}

# Remove old zip if it exists
if (Test-Path $zipPath) {
    Remove-Item $zipPath -Force
}

# Create a temporary staging directory to pack from
$stagingDir = Join-Path $rootDir "temp_staging"
if (Test-Path $stagingDir) {
    Remove-Item $stagingDir -Recurse -Force
}
New-Item -ItemType Directory -Path $stagingDir -Force | Out-Null

# Copy required files and folders to staging
Copy-Item (Join-Path $rootDir "bin\zenith-setup.exe") $stagingDir
Copy-Item (Join-Path $rootDir "bin\zenith-shield.exe") $stagingDir
Copy-Item (Join-Path $rootDir "bin\zenith-app.exe") $stagingDir

# Create extension directory inside staging area
$stagingExtensionDir = Join-Path $stagingDir "extension"
New-Item -ItemType Directory -Path $stagingExtensionDir -Force | Out-Null

# Copy extension files to extension subfolder in staging
Copy-Item (Join-Path $rootDir "extension\manifest.json") $stagingExtensionDir
Copy-Item (Join-Path $rootDir "extension\background.js") $stagingExtensionDir
Copy-Item (Join-Path $rootDir "extension\content_scanner.js") $stagingExtensionDir
Copy-Item (Join-Path $rootDir "extension\prompt_guard.js") $stagingExtensionDir
Copy-Item (Join-Path $rootDir "extension\extension_id.txt") $stagingExtensionDir

Copy-Item (Join-Path $rootDir "extension\dashboard") $stagingExtensionDir -Recurse
Copy-Item (Join-Path $rootDir "extension\intervention") $stagingExtensionDir -Recurse
Copy-Item (Join-Path $rootDir "extension\popup") $stagingExtensionDir -Recurse
Copy-Item (Join-Path $rootDir "extension\icons") $stagingExtensionDir -Recurse
Copy-Item (Join-Path $rootDir "extension\utils") $stagingExtensionDir -Recurse

# Compress staging directory content into zip
Compress-Archive -Path "$stagingDir\*" -DestinationPath $zipPath -Force

# Clean up staging directory
Remove-Item $stagingDir -Recurse -Force

# Copy the fresh zip to the external zenith-website folder too, keeping it updated
$externalWebsiteZip = "C:\Users\itsYurtzy\.gemini\antigravity\scratch\zenith-website\downloads\zenith-anti-pmo.zip"
if (Test-Path (Split-Path $externalWebsiteZip)) {
    Copy-Item $zipPath $externalWebsiteZip -Force
    Write-Host "Also updated external zenith-website downloads ZIP." -ForegroundColor Gray
}

$size = (Get-Item $zipPath).Length
$sizeKb = [Math]::Round($size / 1024, 2)
Write-Host "`n[SUCCESS] Successfully compiled and packaged Zenith Suite to $zipPath" -ForegroundColor Green
Write-Host "Distribution Package Size: $sizeKb KB" -ForegroundColor Green
