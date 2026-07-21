namespace Foop.Models;

internal sealed record MonitorDescriptor(
    nint Handle,
    string DeviceName,
    string DisplayName,
    ScreenRect Bounds,
    ScreenRect WorkArea,
    bool IsPrimary)
{
    internal string DisplayNumber => GetDisplayNumber(DeviceName);

    internal static string GetDisplayNumber(string deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            return "?";
        }

        var firstDigit = deviceName.Length;
        while (firstDigit > 0 && char.IsDigit(deviceName[firstDigit - 1]))
        {
            firstDigit--;
        }

        return firstDigit < deviceName.Length
            ? deviceName[firstDigit..]
            : "?";
    }
}
