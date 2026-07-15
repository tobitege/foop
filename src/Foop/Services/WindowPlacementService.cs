using System.ComponentModel;
using System.Runtime.InteropServices;
using Foop.Interop;
using Foop.Models;

namespace Foop.Services;

internal static class WindowPlacementService
{
    internal static ScreenRect CenterWithin(ScreenRect window, ScreenRect workArea, bool constrainToWorkArea)
    {
        var width = constrainToWorkArea ? Math.Min(window.Width, workArea.Width) : window.Width;
        var height = constrainToWorkArea ? Math.Min(window.Height, workArea.Height) : window.Height;
        var x = workArea.X + ((workArea.Width - width) / 2);
        var y = workArea.Y + ((workArea.Height - height) / 2);
        return new ScreenRect(x, y, width, height);
    }

    internal static void CenterWindow(
        nint windowHandle,
        MonitorDescriptor monitor,
        bool constrainToWorkArea)
    {
        if (!NativeMethods.GetWindowRect(windowHandle, out var currentRect))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var current = new ScreenRect(
            currentRect.Left,
            currentRect.Top,
            currentRect.Right - currentRect.Left,
            currentRect.Bottom - currentRect.Top);
        var target = CenterWithin(current, monitor.WorkArea, constrainToWorkArea);
        var flags = NativeMethods.SwpNoZOrder |
                    NativeMethods.SwpNoOwnerZOrder |
                    NativeMethods.SwpNoActivate;

        if (!NativeMethods.SetWindowPos(
                windowHandle,
                nint.Zero,
                target.X,
                target.Y,
                target.Width,
                target.Height,
                flags))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    internal static bool TryPositionProxy(nint windowHandle, MonitorDescriptor monitor)
    {
        var flags = NativeMethods.SwpNoZOrder |
                    NativeMethods.SwpNoOwnerZOrder |
                    NativeMethods.SwpNoActivate;
        return NativeMethods.SetWindowPos(
            windowHandle,
            nint.Zero,
            monitor.WorkArea.X + 1,
            monitor.WorkArea.Y + 1,
            1,
            1,
            flags);
    }
}
