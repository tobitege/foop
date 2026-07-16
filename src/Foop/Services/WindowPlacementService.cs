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

    internal static ScreenRect PlaceWithin(ScreenRect window, ScreenRect workArea)
    {
        var width = Math.Min(Math.Max(1, window.Width), Math.Max(1, workArea.Width));
        var height = Math.Min(Math.Max(1, window.Height), Math.Max(1, workArea.Height));
        var maxX = workArea.X + workArea.Width - width;
        var maxY = workArea.Y + workArea.Height - height;
        var x = Math.Clamp(window.X, workArea.X, Math.Max(workArea.X, maxX));
        var y = Math.Clamp(window.Y, workArea.Y, Math.Max(workArea.Y, maxY));
        return new ScreenRect(x, y, width, height);
    }

    internal static bool TryGetNormalBounds(nint windowHandle, out ScreenRect bounds)
    {
        bounds = default;
        var placement = new NativeMethods.WindowPlacement
        {
            Length = Marshal.SizeOf<NativeMethods.WindowPlacement>()
        };

        if (!NativeMethods.GetWindowPlacement(windowHandle, ref placement))
        {
            return false;
        }

        var normal = placement.NormalPosition;
        var width = normal.Right - normal.Left;
        var height = normal.Bottom - normal.Top;
        if (width <= 0 || height <= 0)
        {
            return false;
        }

        bounds = new ScreenRect(normal.Left, normal.Top, width, height);
        return true;
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
        ApplyWindowRect(windowHandle, target);
    }

    internal static bool TryPlaceFromWorkAreaOffset(
        nint windowHandle,
        MonitorDescriptor monitor,
        double? offsetX,
        double? offsetY,
        bool constrainToWorkArea)
    {
        if (offsetX is not double storedOffsetX
            || offsetY is not double storedOffsetY
            || !WindowSizeConstraints.IsSafeStoredCoordinate(storedOffsetX)
            || !WindowSizeConstraints.IsSafeStoredCoordinate(storedOffsetY))
        {
            return false;
        }

        if (!NativeMethods.GetWindowRect(windowHandle, out var currentRect))
        {
            return false;
        }

        var width = currentRect.Right - currentRect.Left;
        var height = currentRect.Bottom - currentRect.Top;
        var proposed = new ScreenRect(
            monitor.WorkArea.X + (int)Math.Round(storedOffsetX),
            monitor.WorkArea.Y + (int)Math.Round(storedOffsetY),
            width,
            height);
        var target = constrainToWorkArea
            ? PlaceWithin(proposed, monitor.WorkArea)
            : proposed;
        ApplyWindowRect(windowHandle, target);
        return true;
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

    private static void ApplyWindowRect(nint windowHandle, ScreenRect target)
    {
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
}
