namespace Foop.Models;

internal static class DesktopWindowIdentity
{
    private const string PathPrefix = "path:";
    private const string ProcessPrefix = "process:";

    internal static string GetKey(DesktopWindow window) =>
        GetKey(window.ExecutablePath, window.ProcessName);

    internal static string GetKey(string executablePath, string processName)
    {
        var normalizedPath = Normalize(executablePath);
        return !string.IsNullOrEmpty(normalizedPath)
            ? PathPrefix + normalizedPath
            : ProcessPrefix + Normalize(processName);
    }

    internal static bool Matches(AutoMoveRule rule, DesktopWindow window)
    {
        var rulePath = Normalize(rule.ExecutablePath);
        var windowPath = Normalize(window.ExecutablePath);
        if (!string.IsNullOrEmpty(rulePath) && !string.IsNullOrEmpty(windowPath))
        {
            return string.Equals(rulePath, windowPath, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(
            Normalize(rule.ProcessName),
            Normalize(window.ProcessName),
            StringComparison.OrdinalIgnoreCase);
    }

    internal static bool SameApplication(DesktopWindow left, DesktopWindow right)
    {
        var leftPath = Normalize(left.ExecutablePath);
        var rightPath = Normalize(right.ExecutablePath);
        if (!string.IsNullOrEmpty(leftPath) && !string.IsNullOrEmpty(rightPath))
        {
            return string.Equals(leftPath, rightPath, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(
            Normalize(left.ProcessName),
            Normalize(right.ProcessName),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string Normalize(string? value) => value?.Trim() ?? string.Empty;
}
