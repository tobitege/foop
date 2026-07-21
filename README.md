<h1 align="center">Foop</h1>

<p align="center">
  <img src=".github/assets/foop-hero.png" alt="Two cartoon monitors moving an application window from right to left" width="100%">
</p>

Foop is a small .NET 10 Windows desktop utility for multi-monitor setups. It lists active desktop windows and moves a selected window, centered, onto any active monitor.

## Features

- Creates one Foop window per active monitor: secondary monitors use a minimized taskbar button; the primary monitor uses the notification area (system tray) instead.
- Rebuilds those entries when displays are connected, disconnected, switched on, or switched off.
- Shows only visible, user-facing top-level desktop windows; Foop, tool windows, owned dialogs, and cloaked virtual-desktop windows are excluded.
- Offers Detail and Grid views and can list windows by application name or window title.
- Moves the selected window to the current monitor with **Foop!** or a double-click.
- Opens an app action dialog on right-click to send the app to any active monitor.
- Stores per-app monitor preferences and automatically moves the first running instance to the chosen monitor.
- Marks apps with an active monitor preference using a red monitor-number badge.
- Restores minimized or maximized windows before moving them.
- Keeps the moved window inside the target monitor's working area.
- Persists view mode, window layout, tray behavior, startup behavior, and app monitor preferences in the current user's profile.
- Supports **Start with Windows**, **Start minimized**, **Auto-Minimize on Fooping**, **Minimize to Tray**, and **Close to Tray**.
- Runs as a single instance per Windows session.
- Reports Windows access denial, which can occur when the target application runs elevated and Foop does not.

Windows still controls how application buttons are grouped or duplicated through its multi-monitor taskbar settings. Foop places a real top-level window on each active monitor so modes that show buttons on the window's monitor work as intended.

## Requirements

- Windows 10 or Windows 11
- .NET 10 Desktop Runtime for running
- .NET 10 SDK for building

The application uses only .NET/WPF and Windows APIs. It has no external NuGet package dependencies.

## Build and test

Build the solution:

```powershell
dotnet build .\Foop.slnx --configuration Release
```

To build and then run the test project in one step, use `Scripts\verify.ps1`. It calls `build.ps1` (Release `dotnet build` of `Foop.slnx`) and `Scripts\test.ps1` (`dotnet run` on `tests\Foop.Tests`), and writes logs to `artifacts\logs`:

```powershell
pwsh -NoProfile -File .\Scripts\verify.ps1
```

## Run

```powershell
dotnet run --project .\src\Foop\Foop.csproj
```

**Minimize to Tray** and **Close to Tray** independently control whether minimizing or closing any Foop window hides it in the notification area. Use **Exit Foop** in the tray menu to quit.

## Start menu shortcut

Use **Settings → Create Start Menu icon…** to create a shortcut for the current user or all users. The all-users option requests administrator rights.

The same operation is available through PowerShell. In a downloaded release, keep the script beside `Foop.exe`. In the source repository, build Foop first. Then create the shortcut for the current user:

```powershell
pwsh -NoProfile -File .\create-start-menu-shortcut.ps1 -Scope CurrentUser
```

To create the shortcut for all users, run PowerShell as administrator:

```powershell
pwsh -NoProfile -File .\create-start-menu-shortcut.ps1 -Scope AllUsers
```

The shortcut targets the Release build and uses the executable's icon.

## Create a GitHub release

The manual `Release` workflow builds and tests Foop with .NET 10, publishes a self-contained single-file Windows x64 application, and creates a GitHub release with a ZIP archive.

Before starting it:

1. Set the release version in `src/Foop/Foop.csproj`.
2. Add the matching `## [X.Y.Z]` entry to `CHANGELOG.md`.
3. Commit and push the changes to `main`.
4. Open **Actions → Release → Run workflow** and enter the version without the `v` prefix.

The workflow creates the corresponding `vX.Y.Z` tag and release.
