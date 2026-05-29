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

# --- AUTOMATIC FREE LOCAL CODE SIGNING ---
Write-Host "`n3. Running Local Code Signing (Free Verification)..." -ForegroundColor Cyan

try {
    # Check if a certificate for Zenith Focus Suite already exists
    $cert = Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -like "*CN=Zenith Focus Suite*" } | Select-Object -First 1
    
    if (-not $cert) {
        Write-Host "Generating a new self-signed Code Signing Certificate..." -ForegroundColor Yellow
        $cert = New-SelfSignedCertificate -Type CodeSigningCert -Subject "CN=Zenith Focus Suite" -FriendlyName "Zenith Local Code Sign" -CertStoreLocation "Cert:\CurrentUser\My"
        
        # Install to CurrentUser Root Store (Trusted Root Certification Authorities)
        $rootStore = New-Object System.Security.Cryptography.X509Certificates.X509Store("Root", "CurrentUser")
        $rootStore.Open("ReadWrite")
        $rootStore.Add($cert)
        $rootStore.Close()
        
        # Install to CurrentUser TrustedPublisher Store (removes "untrusted publisher" warning)
        $pubStore = New-Object System.Security.Cryptography.X509Certificates.X509Store("TrustedPublisher", "CurrentUser")
        $pubStore.Open("ReadWrite")
        $pubStore.Add($cert)
        $pubStore.Close()
        
        Write-Host "Successfully generated and trusted certificate: CN=Zenith Focus Suite" -ForegroundColor Green
    } else {
        Write-Host "Found existing local certificate: CN=Zenith Focus Suite" -ForegroundColor Gray
    }

    # Sign compiled executables natively
    $binaries = @($shieldOutput, $appOutput, $installerOutput)
    foreach ($bin in $binaries) {
        if (Test-Path $bin) {
            $binName = Split-Path $bin -Leaf
            Write-Host "Signing $binName..." -ForegroundColor Cyan
            Set-AuthenticodeSignature -FilePath $bin -Certificate $cert | Out-Null
            Write-Host "Successfully signed $binName!" -ForegroundColor Green
        }
    }
    Write-Host "`n[SUCCESS] Code-signing completed. All binaries are now locally trusted." -ForegroundColor Green
} catch {
    Write-Host "`n[WARNING] Local code-signing encountered an issue: $_" -ForegroundColor Yellow
    Write-Host "The application will still function, but you may see a Defender warning on launch." -ForegroundColor Yellow
}

