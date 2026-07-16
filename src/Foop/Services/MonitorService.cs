using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Foop.Interop;
using Foop.Models;

namespace Foop.Services;

internal sealed partial class MonitorService
{
    internal IReadOnlyList<MonitorDescriptor> GetActiveMonitors()
    {
        var monitors = new List<MonitorDescriptor>();
        NativeMethods.MonitorEnumProc callback = (handle, _, _, _) =>
        {
            var info = new NativeMethods.MonitorInfoEx
            {
                Size = Marshal.SizeOf<NativeMethods.MonitorInfoEx>()
            };

            if (!NativeMethods.GetMonitorInfo(handle, ref info))
            {
                return true;
            }

            var isPrimary = (info.Flags & NativeMethods.MonitorInfoPrimary) != 0;
            monitors.Add(new MonitorDescriptor(
                handle,
                info.DeviceName,
                CreateDisplayName(info.DeviceName, isPrimary),
                ToScreenRect(info.Monitor),
                ToScreenRect(info.WorkArea),
                isPrimary));
            return true;
        };

        if (!NativeMethods.EnumDisplayMonitors(nint.Zero, nint.Zero, callback, nint.Zero))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        return monitors
            .OrderByDescending(monitor => monitor.IsPrimary)
            .ThenBy(monitor => monitor.Bounds.X)
            .ThenBy(monitor => monitor.Bounds.Y)
            .ToArray();
    }

    internal MonitorDescriptor GetMonitorAtCursor(IReadOnlyList<MonitorDescriptor> monitors)
    {
        if (monitors.Count == 0)
        {
            throw new InvalidOperationException("Windows reported no active monitor.");
        }

        if (NativeMethods.GetCursorPos(out var cursor))
        {
            var handle = NativeMethods.MonitorFromPoint(cursor, NativeMethods.MonitorDefaultToNearest);
            var match = monitors.FirstOrDefault(monitor => monitor.Handle == handle);
            if (match is not null)
            {
                return match;
            }
        }

        return monitors.FirstOrDefault(monitor => monitor.IsPrimary) ?? monitors[0];
    }

    private static ScreenRect ToScreenRect(NativeMethods.Rect rect) =>
        new(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);

    private static string CreateDisplayName(string deviceName, bool isPrimary)
    {
        var match = DisplayNumberRegex().Match(deviceName);
        var number = match.Success ? match.Groups[1].Value : deviceName;
        return isPrimary ? $"Monitor {number} (Primary)" : $"Monitor {number}";
    }

    [GeneratedRegex(@"DISPLAY(\d+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex DisplayNumberRegex();
}
