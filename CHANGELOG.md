# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

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
- Restore placement from Win32 normal bounds in physical pixels; center only when no valid saved offset exists.
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
