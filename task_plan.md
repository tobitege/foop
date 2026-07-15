# Task Plan: Foop desktop window mover

## Goal
Create a modern .NET 10 Windows desktop taskbar application that lists active desktop windows and moves the selected window to the monitor from which Foop was invoked.

## Current Phase
Complete

## Phases

### Phase 1: Requirements and environment discovery
- [x] Capture functional and repository constraints
- [x] Verify installed .NET SDK, templates, and repository state
- [x] Record monitor/taskbar behavior assumptions
- **Status:** complete

### Phase 2: Architecture and project structure
- [x] Select the lowest-dependency Windows UI stack
- [x] Define native window and monitor interop boundaries
- [x] Create solution and project structure
- **Status:** complete

### Phase 3: Implementation
- [x] Implement active desktop-window enumeration
- [x] Implement current-monitor detection and centered window movement
- [x] Implement modern non-editable window list UI and Foop action
- [x] Implement taskbar activation behavior and display-change handling
- [x] Add current-user and all-user Start menu shortcut creation
- **Status:** complete

### Phase 4: Testing and verification
- [x] Build through the repository entry point
- [x] Run focused automated tests for geometry and filtering
- [x] Exercise the application on the Windows desktop where feasible
- [x] Verify encoding, line endings, and dependency footprint
- **Status:** complete

### Phase 5: Delivery
- [x] Review repository contents and documentation
- [x] Mark all planning artifacts complete
- [x] Hand off paths and run instructions
- **Status:** complete

### Phase 6: GitHub build and release workflow
- [x] Inspect the Flowery.NET CI/release workflow and related repository configuration
- [x] Adapt the workflow to Foop, .NET 10, and the Windows desktop output
- [x] Validate workflow syntax, paths, versioning, artifacts, and permissions
- [x] Document the release trigger and produced assets
- **Status:** complete

## Key Questions
1. How can activation identify the taskbar/monitor context reliably within Windows limitations?
2. Which windows qualify as user-facing desktop applications?
3. How should minimized, maximized, elevated, cloaked, and disconnected-monitor windows be handled?

## Decisions Made
| Decision | Rationale |
|----------|-----------|
| Target .NET 10 and Windows only | Required by the user and native window management is Windows-specific |
| Prefer WPF unless environment discovery invalidates it | Ships with the .NET Windows Desktop runtime and avoids third-party UI dependencies |
| Keep native interop in a dedicated service | Separates Win32 policy from UI and makes geometry/filtering testable |

## Errors Encountered
| Error | Attempt | Resolution |
|-------|---------|------------|
| `apply_patch` could not match the BOM-prefixed first line of `App.xaml` | 1 | Rebuild patches in smaller groups and anchor edits after the first line |
| WPF startup failed because `FontCache.Util` received no valid process-level `windir` | 1-2 | Add an explicit entry point that repairs the process-only value from `SystemRoot` before WPF initializes |
| `Program.cs` missed `System.IO`, and `verify.ps1` did not stop after its child build failed | 1 | Add the namespace and make build/test scripts throw on nonzero exit codes |
| Taskbar proxy stayed unarmed because an empty window never raised `ContentRendered` | 1 | Arm the proxy from `SourceInitialized` via the dispatcher |
| Windows taskbar UI Automation did not activate transparent 1×1 proxy windows reliably | 2 | Use one real, minimized Foop window per active monitor so Windows restores the UI directly |
| Closing one monitor window synchronously closed it again from its own `Closing` event | 1 | Queue coordinated application shutdown after the closing event returns |

## Notes
- BOM conversion applies only to .cs, .csproj, and .axaml, once at the end of the iteration.
- Never permanently delete files; use the configured Recycle Bin script for removals.
- Use only the documented repository build/test entry point once established.
