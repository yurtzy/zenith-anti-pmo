# Zenith Focus Suite

Zenith is a premium self-control and productivity suite designed to prevent digital compulsion. By combining a lightweight Chromium browser extension with native C# background utilities, Zenith offers multi-layered system-wide protection that cannot be easily closed or bypassed.

---

## Technical Architecture

Zenith operates using a hybrid software model:

1. **Chromium Extension**: Monitors local WebUI interfaces (such as ComfyUI, Stable Diffusion, CivitAI) and standard search engines, blocking suggestive AI prompt generation and diverting triggered attempts.
2. **Native C# Shield (`zenith-shield.exe`)**: A windowless background process that monitors active application titles and browser instances, instantly terminating forbidden items and redirecting the user to a mindful pause screen.
3. **Double-Process Mutual Keep-Alive Watchdog**: When enabled in the setup installer, the Shield spawns a child watchdog process. The main process and the watchdog monitor each other's PIDs; if either is terminated via Task Manager, the survivor instantly resurrects the other.
4. **App Launcher (`zenith-app.exe`)**: A borderless desktop shell that launches the extension's interactive Overview Dashboard in standalone application mode.
5. **Setup Wizard (`zenith-setup.exe`)**: A modern, borderless installer that manages file directories, shortcut creation, startup configurations, and optional Watchdog keep-alive protection.

---

## Repository Structure

```text
├── dashboard/        # Chrome Extension Dashboard UI (Overview, Streak, Journal)
├── docs/             # Web Landing Page & ZIP distribution bundle (GitHub Pages root)
│   └── downloads/    # Packaged production zip archive
├── icons/            # Distraction-free monochrome application graphics
├── intervention/     # Navy SEAL box breathing redirection interface
├── popup/            # Extension status bar control window
├── shield/           # Native C# Source Code & Scripts
│   ├── App.cs        # Standalone App Launcher source
│   ├── compile.ps1   # Native .NET compiler execution script
│   ├── Installer.cs  # Zenith Setup Wizard GUI source
│   ├── pack.ps1      # Unified compilation and zip packaging script
│   ├── Shield.cs     # Background process guard and watchdog monitor source
│   └── zenith.ico    # Mathematically-drawn monochrome Zen icon
├── utils/            # General purpose text filters and helper files
├── background.js     # Chrome Extension event listener & page redirects
├── manifest.json     # Chrome Extension metadata manifest v3
└── extension_id.txt  # Hardcoded extension ID mapping file
```

---

## Building and Packaging

Zenith requires the standard Microsoft .NET Framework C# compiler (`csc.exe`), which is installed natively on all Windows systems.

To automatically recompile all three C# binaries and package the entire distribution suite into the website download zip, open a PowerShell console in the repository root and run:

```powershell
Set-Location .\shield
powershell.exe -ExecutionPolicy Bypass -File .\pack.ps1
```

The packing script will:
1. Compile `zenith-shield.exe`, `zenith-app.exe`, and `zenith-setup.exe`.
2. Move the binaries to the root directory.
3. Clean and stage the production-only files (executables, extension assets, and folders) into a temporary workspace.
4. Package the staging directory into `docs/downloads/zenith-anti-pmo.zip`.
5. Remove temporary folders.

---

## Installation Guide

To install the Zenith Suite on your system:

1. Download or clone this repository to a local directory (e.g., `C:\Zenith`).
2. Run `zenith-setup.exe` and select your components:
   * **Background Shield**: Runs in the background to prevent browser bypasses.
   * **Desktop App Launcher**: Creates shortcuts to manage your statistics.
   * **Watchdog Keep-Alive**: Activates the double-process protection.
3. Advance through the installation. The setup program will copy the necessary files and copy the installation directory path to your system clipboard.
4. Open Google Chrome and navigate to `chrome://extensions/`.
5. Enable **Developer Mode** (toggle switch in the top-right corner).
6. Click **Load unpacked** (button in the top-left corner).
7. Paste the copied path from your clipboard (`Ctrl+V`) and select the folder.
8. Click **Details** on the Zenith card and toggle **Allow in incognito** to enable full protection.

---

## Development and Customization

### Modifying Trigger Categories
To alter the text targets monitored system-wide, update the string array in `shield/Shield.cs`:
```csharp
private static readonly string[] Triggers = new string[] { ... };
```
Recompile the suite using `pack.ps1` to apply the updates.

### Deactivation & Uninstallation Guide

Zenith runs a mutual-survival keep-alive watchdog to prevent bypasses. To deactivate the watchdog and uninstall the suite cleanly:

1. **Obtain Hardware ID**: Run `zenith-setup.exe` and click the **Uninstall** button at the bottom-left corner of the Welcome screen. Copy your system's unique Hardware ID (e.g. `ZEN-XXXX-XXXX`).
2. **Request Deactivation Key**: Email your Hardware ID to `zenith.suite.help@outlook.com` to request a one-time Deactivation Key.
3. **Execute Deactivation**: Input the key (e.g. `KEY-XXXX-XXXX-XXXX-XXXX`) into the textbox and click **Validate & Remove**. The installer will stop all background watchdog processes, clean up local files, and remove Desktop/Startup shortcuts automatically.

*Note: For emergency manual uninstalls, you can stop the watchdog loop by running `taskkill /f /im zenith-shield.exe` in a command console to terminate both guard processes simultaneously, unlocking the installation directory files for manual deletion.*

