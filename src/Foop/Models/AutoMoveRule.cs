namespace Foop.Models;

internal sealed record AutoMoveRule(
    string ExecutablePath,
    string ProcessName,
    string MonitorDeviceName);
