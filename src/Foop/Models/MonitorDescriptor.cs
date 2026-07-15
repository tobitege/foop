namespace Foop.Models;

internal sealed record MonitorDescriptor(
    nint Handle,
    string DeviceName,
    string DisplayName,
    ScreenRect Bounds,
    ScreenRect WorkArea,
    bool IsPrimary);
