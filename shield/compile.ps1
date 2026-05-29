# Zenith Anti-PMO - Unified C# Compilation Script
# Natively compiles the windowless Shield, the Standalone App Launcher, and the premium borderless Setup Wizard Installer.

$compiler = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$shieldSource = Join-Path $PSScriptRoot "Shield.cs"
$appSource = Join-Path $PSScriptRoot "App.cs"
$installerSource = Join-Path $PSScriptRoot "Installer.cs"

$shieldOutput = Join-Path $PSScriptRoot "..\bin\zenith-shield.exe"
$appOutput = Join-Path $PSScriptRoot "..\bin\zenith-app.exe"
$installerOutput = Join-Path $PSScriptRoot "..\bin\zenith-setup.exe"

if (-not (Test-Path $compiler)) {
    Write-Error "Microsoft .NET C# Compiler (csc.exe) not found at standard path: $compiler"
    exit 1
}

# 1. Compile Zenith Shield (Silent background process-monitor)
Write-Host "Compiling Zenith Shield..." -ForegroundColor Cyan
& $compiler /target:winexe /optimize /win32icon:zenith.ico /out:"$shieldOutput" "$shieldSource"

if ($LASTEXITCODE -eq 0) {
    $size = (Get-Item $shieldOutput).Length
    $sizeKb = [Math]::Round($size / 1024, 2)
    Write-Host "Zenith Shield compiled successfully! Size: $sizeKb KB" -ForegroundColor Green
} else {
    Write-Error "Compilation failed for Shield.cs"
    exit 1
}

# 2. Compile Zenith Standalone App Launcher (Borderless GUI shell)
Write-Host "Compiling Zenith Desktop App Launcher..." -ForegroundColor Cyan
& $compiler /target:winexe /optimize /win32icon:zenith.ico /out:"$appOutput" "$appSource"

if ($LASTEXITCODE -eq 0) {
    $size = (Get-Item $appOutput).Length
    $sizeKb = [Math]::Round($size / 1024, 2)
    Write-Host "Zenith Desktop App Launcher compiled successfully! Size: $sizeKb KB" -ForegroundColor Green
} else {
    Write-Error "Compilation failed for App.cs"
    exit 1
}

# 3. Compile Zenith Setup Installer Wizard (Borderless GUI setup program)
Write-Host "Compiling Zenith Setup Installer..." -ForegroundColor Cyan
& $compiler /target:winexe /r:System.Windows.Forms.dll /r:System.Drawing.dll /optimize /win32icon:zenith.ico /win32manifest:setup.manifest /out:"$installerOutput" "$installerSource"

if ($LASTEXITCODE -eq 0) {
    $size = (Get-Item $installerOutput).Length
    $sizeKb = [Math]::Round($size / 1024, 2)
    Write-Host "Zenith Setup Installer compiled successfully! Size: $sizeKb KB" -ForegroundColor Green
} else {
    Write-Error "Compilation failed for Installer.cs"
    exit 1
}

Write-Host "`nAll Zenith C# components compiled natively and stored in bin directory successfully." -ForegroundColor Green
