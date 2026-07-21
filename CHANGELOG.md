# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2026-07-22

### Fixed

- Keep secondary-monitor Foop taskbar buttons during startup when **Minimize to Tray** is enabled.
- Apply **Start minimized** only to Windows logon autostart, so Start-menu launches still open Foop on the monitor under the cursor.

### Changed

- Bump application version to 1.0.1.

## [1.0.0] - 2026-07-21

### Added

- Settings option **List by application name**: show the application name as the primary label and the window title as secondary text.
- Resolve a friendlier application name from the process file description (or product name) when available.
- Notification-area (tray) icon for the primary monitor instead of a taskbar button.
- Tray menu toggles for **Start with Windows**, **Start minimized**, **Minimize to Tray**, and **Close to Tray**, then a separator before **Open Foop** / **Exit Foop**.
- Settings options for **Start minimized** (default off), **Minimize to Tray**, and **Close to Tray**.
- After an elevated restart for an all-users Start menu shortcut, bring Foop to the foreground and confirm before creating it.
- Keep each Foop window confined to its assigned monitor work area (no dragging onto another display).
- Show the application version in the Settings dialog footer.
- **Create Start Menu icon** action in Settings with scope selection (current user or all users).
- Open an app action dialog by right-clicking a listed desktop app.
- Send an app to any active monitor from the app action dialog.
- Create or disable a persistent rule that moves the first running instance of an app to a chosen monitor.
- Identify auto-move rules by executable path, with the process name as a fallback when the path is unavailable.
- Show the preferred monitor number as a red badge on app list and grid entries.

### Changed

- Default listing mode prefers application names over window titles.
- Apply minimize-to-tray and close-to-tray behavior to Foop windows on every monitor.
- Secondary-monitor Foop windows remain on that monitor's taskbar; startup and tray **Open Foop** present the Foop window for the monitor under the cursor.
- Persist and restore window size in DIP after the window handle exists so DPI scaling does not fall back to defaults.
- Replace the Start Menu icon checkbox with an explicit create action (no longer a persisted toggle).
- Bump application version to 1.0.0.

### Fixed

- Prevent Foop windows from flashing during startup by showing secondary windows minimized and leaving the primary window unshown.
- Prevent more than one Foop instance from running in the same Windows session.

## [0.1.1] - 2026-07-16

### Added

- Double-click a listed application to run the Foop action.
- View switcher with Detail (list) and Grid (wrapping tile grid, max 200 px wide).
- Settings dialog with Start with Windows, Create Start Menu icon, and Auto-Minimize on Fooping.
- Persist settings under the user profile (`%AppData%\Foop\settings.json`) and load them at startup.
- Persist the selected View mode (Detail or Grid).
- Persist the last shared window size and work-area-relative position; restore them on startup.
- Skip the Foop move when the selected window is already on the target monitor.
- Foop! action button with the Foop icon and a raised 3D press effect.
- Footer with a link to the GitHub repository and an MIT license notice.
- MIT `LICENSE` file at the repository root.

### Changed

- Use English for all in-app text.
- Center the repository footer line.
- Restore placement from Win32 normal bounds; center only when no valid saved work-area offset exists.
- Persist window bounds only from the Foop window the user closes, so other monitor proxies cannot overwrite it.
- Bump application version to 0.1.1.

## [0.1.0] - 2026-07-15

### Added

- List active desktop windows in a clear, read-only view.
- Move the selected window to the center of the chosen monitor.
- Make Foop available in the taskbar of every active monitor.
- Detect changes to the active monitor configuration automatically.
- Restore minimized and maximized windows before moving them.
- Provide a modern WPF interface without external NuGet dependencies.
- Create a Start menu shortcut for the current user or all users.
