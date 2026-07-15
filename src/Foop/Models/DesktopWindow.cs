namespace Foop.Models;

internal sealed record DesktopWindow(
    nint Handle,
    string Title,
    string ProcessName,
    uint ProcessId,
    bool IsMinimized)
{
    public string Initial => string.IsNullOrWhiteSpace(ProcessName)
        ? "?"
        : char.ToUpperInvariant(ProcessName[0]).ToString();

    public string ProcessDisplayName => $"{ProcessName}  ·  PID {ProcessId}";

    public string StateLabel => IsMinimized ? "Minimiert" : string.Empty;
}
