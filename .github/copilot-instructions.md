# GitHub Copilot Repository Instructions

This document provides system-level context and instructions for GitHub Copilot when assisting developers with writing code, reviewing pull requests, creating issues, or planning tasks in the **Zenith Focus Suite** repository.

---

## 1. Project Overview & Architecture

Zenith is a premium self-control and productivity suite designed to prevent digital compulsions. It uses a hybrid software model:
1. **Native C# Shield (`zenith-shield.exe`)**: A lightweight background service monitoring system-wide active window titles. If a forbidden term is detected, it terminates the process and opens an intervention screen.
2. **Watchdog Keep-Alive**: When enabled, the Shield process and a child watchdog process monitor each other's PIDs. If one is killed, the survivor instantly resurrects it.
3. **Setup Wizard (`zenith-setup.exe`)**: A WinForms installer wizard for managing files, shortcuts, and startup keys.
4. **App Launcher (`zenith-app.exe`)**: A borderless desktop shell to launch the extension's dashboard in standalone mode.
5. **Chrome Extension**: Monitors browser WebUIs and blocks suggestive AI generation prompts.

---

## 2. General Coding Standards

### C# Components (Setup, Shield, Launcher)
- **Framework**: Targeting standard .NET Framework (compiled with `csc.exe` v4.0.30319). Do not use modern .NET Core / .NET 6+ features that break native compatibility without dependencies.
- **Assembly Information**: All C# binaries must declare assembly metadata in their source code files (e.g. `[assembly: AssemblyCompany("yurtzy")]`, `[assembly: AssemblyCopyright("Copyright © 2026 yurtzy")]`). **Never include email addresses** (e.g., do not expose `lionelgaming256@gmail.com`) or personal identifiers in any source code, logs, or commit messages.
- **UAC Manifest**: Always compile `zenith-setup.exe` with the custom `setup.manifest` (`/win32manifest:setup.manifest`) to set the execution level to `asInvoker`. This bypasses Windows UAC installer heuristics.
- **GUI Design**: Maintain a dark mode, borderless, matte-black aesthetic (Color `10, 10, 12` background, white/gray text, blue/green accent highlights).

### Chrome Extension Components
- **Manifest**: Follow Chrome Manifest V3 specifications.
- **Javascript**: Use vanilla asynchronous Javascript with standard Chromium/Chrome API methods (`chrome.runtime`, `chrome.tabs`, etc.). No external runtime frameworks.

---

## 3. Pull Request Guidelines

When Copilot generates descriptions, summaries, or reviews for Pull Requests:
- **Describe Executable Effects**: Clearly describe how changes affect the behavior of `zenith-shield.exe`, `zenith-setup.exe`, or `zenith-app.exe`.
- **Verify Manifest Compliance**: Flag any modifications to the installer that omit the `/win32manifest` option in build scripts.
- **Privacy Check**: Ensure no debugging logs or comments expose sensitive information or email addresses.
- **Verify State Preservation**: In UI steps, check that navigation controls preserve state (like checkbox options) when users go back and forward.

---

## 4. Issue and Task Management

When Copilot assists with planning, drafting, or resolving issues:
- **Lightweight Architecture**: Prioritize low memory footprint. All executables combined must remain under 100 KB in size.
- **Watchdog Stability**: Any proposed issue fix in the Keep-Alive Watchdog loop (`Shield.cs`) must avoid deadlocks, CPU spikes (ensure appropriate thread sleeps), or infinite resurrection loops.
- **Clear Installation Impact**: Highlight whether changes require updating the Setup Wizard flow, updating shortcuts, or updating files packaged into the distribution ZIP.
