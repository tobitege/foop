# Findings and Decisions

## Requirements
- New C# .NET 10 desktop application in `D:\github\foop`.
- Designed for multi-monitor use and must tolerate one or more configured displays being switched off/disconnected.
- Foop must be reachable from every taskbar/monitor context.
- Activating Foop shows a modern window on the current monitor.
- The UI lists active desktop applications in a non-editable selection list.
- Clicking `Foop!` moves the selected application window, centered, to the current monitor.
- Keep dependencies to an absolute minimum.
- Provide a Start menu shortcut for either the current user or all users.

## Research Findings
- The requested target directory did not exist; `D:\github` exists.
- No parent `D:\github\AGENTS.md` exists. The supplied and global Codex instructions apply.
- RTK is available at `C:\Users\tobias\.rtk\rtk.exe`.
- Installed .NET 10 SDKs include 10.0.109, 10.0.203, 10.0.204, 10.0.301, and 10.0.302; the WPF app template is installed.
- Windows assigns taskbar presence to top-level windows, while multi-monitor taskbar placement remains governed by the user's Windows mode. A real minimized Foop window on every active monitor provides per-monitor presence even in the "window is on" modes.
- `EnumDisplayMonitors` returns the active monitor topology; Foop can rebuild its window set when `DisplaySettingsChanged` fires and never retain unavailable monitor handles.
- WPF on .NET 9+ includes the Windows 11-style Fluent theme and system light/dark mode without a third-party UI package.
- Flowery.NET provides reusable workflow references under `.github/workflows`, including `ci.yml` and `release.yml`; both must be inspected before adapting the release flow.
- Flowery.NET releases are manually triggered, validate a semantic version, require a matching version in project metadata, build with .NET 10, extract the matching `CHANGELOG.md` section, and create a GitHub release.
- Foop is Windows-only WPF, so its release must run on a Windows runner and publish `win-x64`; Flowery.NET's Linux library, NuGet, Android, and gallery jobs do not apply.
- Foop already stores version `0.1.0` in `src/Foop/Foop.csproj`, pins the .NET 10 SDK through `global.json`, and has a matching Keep a Changelog entry.
- The release ZIP must include `create-start-menu-shortcut.ps1`; the script now resolves a colocated portable `Foop.exe` before falling back to the repository build output.
- Static validation confirms the workflow YAML parses, all five multiline PowerShell blocks compile as script blocks, and both shortcut scopes remain valid under `-WhatIf`.
- GitHub documents `windows-2025` for both public and private repositories; the Flowery.NET-specific `windows-2025-vs2026` label is not listed for private repositories, so Foop uses `windows-2025`.
- The `softprops/action-gh-release@v3` documentation confirms Windows support, `contents: write`, changelog `body_path`, asset globs, and `target_commitish`-based tag creation.
- The local repository currently has no configured Git remote, so the workflow can only be exercised after the repository is connected and pushed to GitHub.

## Technical Decisions
| Decision | Rationale |
|----------|-----------|
| Use WPF with custom resources as the initial UI choice | Modern visuals can be implemented with inbox .NET desktop assemblies and no UI package |
| Enumerate top-level Win32 windows | This represents active desktop applications more accurately than process enumeration |
| Bind each Foop window to its enumerated active monitor | Handles displays that are currently unavailable and avoids retaining stale monitor handles |
| Create one minimized Foop window per active monitor | Gives Foop a real taskbar window physically associated with every current monitor and identifies which monitor was used |
| Use the inbox WPF Fluent theme plus a small app-specific resource layer | Follows system light/dark settings with no UI dependency |
| Create Start menu entries through a PowerShell shortcut script | Supports current-user and all-user scope without an installer dependency |
| Use one manual Windows release job | Keeps the adapted workflow small while validating, testing, publishing, packaging, and releasing in one traceable job |
| Publish a self-contained single-file `win-x64` build | Matches the Flowery.NET desktop release pattern and avoids requiring a separate .NET runtime installation |
| Let the release action create `vX.Y.Z` from the selected `main` commit | Avoids a separate mutable tag-push step while retaining the Flowery.NET manual release model |

## Issues Encountered
| Issue | Resolution |
|-------|------------|

## Resources
- `C:\Users\tobias\.codex\AGENTS.md`
- `C:\Users\tobias\.codex\RTK.md`
- https://learn.microsoft.com/en-us/dotnet/desktop/wpf/controls/styles-templates-overview
- https://learn.microsoft.com/en-us/windows/apps/develop/settings/settings-common
- https://learn.microsoft.com/en-us/windows/win32/gdi/multiple-display-monitors-functions

## Visual/Browser Findings
- None.
