using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Foop.Interop;
using Foop.Models;

namespace Foop.Services;

internal sealed class DesktopWindowService
{
    private readonly uint _currentProcessId = (uint)Environment.ProcessId;

    internal IReadOnlyList<DesktopWindow> GetDesktopWindows()
    {
        var windows = new List<DesktopWindow>();
        var shellWindow = NativeMethods.GetShellWindow();
        NativeMethods.EnumWindowsProc callback = (windowHandle, _) =>
        {
            if (TryCreateDesktopWindow(windowHandle, shellWindow, out var desktopWindow))
            {
                windows.Add(desktopWindow);
            }

            return true;
        };

        if (!NativeMethods.EnumWindows(callback, nint.Zero))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return windows
            .OrderBy(window => window.ProcessName, StringComparer.CurrentCultureIgnoreCase)
            .ThenBy(window => window.Title, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
    }

    internal WindowMoveResult MoveToMonitor(DesktopWindow window, MonitorDescriptor monitor)
    {
        if (!NativeMethods.IsWindow(window.Handle))
        {
            return new WindowMoveResult(false, "The selected window is no longer open.");
        }

        var windowMonitor = NativeMethods.MonitorFromWindow(
            window.Handle,
            NativeMethods.MonitorDefaultToNearest);
        if (windowMonitor == monitor.Handle)
        {
            return new WindowMoveResult(
                true,
                $"\"{window.Title}\" is already on {monitor.DisplayName}.",
                Relocated: false);
        }

        try
        {
            if (NativeMethods.IsIconic(window.Handle) || NativeMethods.IsZoomed(window.Handle))
            {
                NativeMethods.ShowWindow(window.Handle, NativeMethods.SwRestore);
            }

            WindowPlacementService.CenterWindow(window.Handle, monitor, constrainToWorkArea: true);
            NativeMethods.ShowWindow(window.Handle, NativeMethods.SwShow);
            NativeMethods.SetForegroundWindow(window.Handle);
            return new WindowMoveResult(true, $"\"{window.Title}\" was moved to {monitor.DisplayName}.");
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == 5)
        {
            return new WindowMoveResult(
                false,
                "Windows denied access. The target application may be running elevated.");
        }
        catch (Win32Exception exception)
        {
            return new WindowMoveResult(false, $"The window could not be moved: {exception.Message}");
        }
    }

    private bool TryCreateDesktopWindow(
        nint windowHandle,
        nint shellWindow,
        out DesktopWindow desktopWindow)
    {
        desktopWindow = null!;
        if (windowHandle == shellWindow || !NativeMethods.IsWindowVisible(windowHandle))
        {
            return false;
        }

        var titleLength = NativeMethods.GetWindowTextLength(windowHandle);
        if (titleLength <= 0)
        {
            return false;
        }

        var extendedStyle = NativeMethods.GetWindowLongPtr(windowHandle, NativeMethods.GwlExStyle).ToInt64();
        var isToolWindow = (extendedStyle & NativeMethods.WsExToolWindow) != 0;
        var isAppWindow = (extendedStyle & NativeMethods.WsExAppWindow) != 0;
        if (isToolWindow || (NativeMethods.GetWindow(windowHandle, NativeMethods.GwOwner) != nint.Zero && !isAppWindow))
        {
            return false;
        }

        var cloakedResult = NativeMethods.DwmGetWindowAttribute(
            windowHandle,
            NativeMethods.DwmwaCloaked,
            out var cloaked,
            sizeof(int));
        if (cloakedResult == 0 && cloaked != 0)
        {
            return false;
        }

        NativeMethods.GetWindowThreadProcessId(windowHandle, out var processId);
        if (processId == 0 || processId == _currentProcessId)
        {
            return false;
        }

        var title = new StringBuilder(titleLength + 1);
        if (NativeMethods.GetWindowText(windowHandle, title, title.Capacity) == 0 ||
            string.IsNullOrWhiteSpace(title.ToString()))
        {
            return false;
        }

        desktopWindow = new DesktopWindow(
            windowHandle,
            title.ToString().Trim(),
            GetProcessName(processId),
            processId,
            NativeMethods.IsIconic(windowHandle));
        return true;
    }

    private static string GetProcessName(uint processId)
    {
        try
        {
            using var process = Process.GetProcessById((int)processId);
            return process.ProcessName;
        }
        catch (ArgumentException)
        {
            return "Application";
        }
        catch (InvalidOperationException)
        {
            return "Application";
        }
        catch (Win32Exception)
        {
            return "Application";
        }
    }
}

internal sealed record WindowMoveResult(bool Success, string Message, bool Relocated = true);
