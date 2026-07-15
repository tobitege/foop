<h1 align="center">Foop</h1>

<p align="center">
  <img src=".github/assets/foop-hero.png" alt="Two cartoon monitors moving an application window from right to left" width="100%">
</p>

Foop is a small .NET 10 Windows desktop utility for multi-monitor setups. It lists active desktop windows and moves the selected window, centered, onto the monitor whose taskbar was used to open Foop.

## Behavior

- Creates one minimized Foop window on every monitor Windows currently reports as active.
- Rebuilds those taskbar entries when displays are connected, disconnected, switched on, or switched off.
- Shows only visible, user-facing top-level desktop windows; Foop, tool windows, owned dialogs, and cloaked virtual-desktop windows are excluded.
- Restores minimized or maximized windows before moving them.
- Keeps the moved window inside the target monitor's working area.
- Reports Windows access denial, which can occur when the target application runs elevated and Foop does not.

Windows still controls how application buttons are grouped or duplicated through its multi-monitor taskbar settings. Foop places a real top-level window on each active monitor so modes that show buttons on the window's monitor work as intended.

## Requirements

- Windows 10 or Windows 11
- .NET 10 Desktop Runtime for running
- .NET 10 SDK for building

The application uses only .NET/WPF and Windows APIs. It has no external NuGet package dependencies.

## Build and test

```powershell
pwsh -NoProfile -File .\verify.ps1
```

Build and test logs are written to `artifacts\logs`.

## Run

```powershell
dotnet run --project .\src\Foop\Foop.csproj
```

Closing any visible Foop window exits the application.

## Start menu shortcut

In a downloaded release, keep the script beside `Foop.exe`. In the source repository, build Foop first. Then create the shortcut for the current user:

```powershell
pwsh -NoProfile -File .\create-start-menu-shortcut.ps1 -Scope CurrentUser
```

To create the shortcut for all users, run PowerShell as administrator:

```powershell
pwsh -NoProfile -File .\create-start-menu-shortcut.ps1 -Scope AllUsers
```

The shortcut targets the selected build configuration (`Release` by default) and uses the executable's icon.

## Create a GitHub release

The manual `Release` workflow builds and tests Foop with .NET 10, publishes a self-contained single-file Windows x64 application, and creates a GitHub release with a ZIP archive.

Before starting it:

1. Set the release version in `src/Foop/Foop.csproj`.
2. Add the matching `## [X.Y.Z]` entry to `CHANGELOG.md`.
3. Commit and push the changes to `main`.
4. Open **Actions → Release → Run workflow** and enter the version without the `v` prefix.

The workflow creates the corresponding `vX.Y.Z` tag and release.
