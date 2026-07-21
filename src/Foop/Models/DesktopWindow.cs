namespace Foop.Models;

internal sealed record DesktopWindow(
    nint Handle,
    string Title,
    string ProcessName,
    string ApplicationName,
    string ExecutablePath,
    uint ProcessId,
    bool IsMinimized,
    bool ListByApplicationName)
{
    public string Initial
    {
        get
        {
            var source = ListByApplicationName ? ApplicationName : Title;
            if (string.IsNullOrWhiteSpace(source))
            {
                source = ProcessName;
            }

            return string.IsNullOrWhiteSpace(source)
                ? "?"
                : char.ToUpperInvariant(source.Trim()[0]).ToString();
        }
    }

    public string PrimaryLabel => ListByApplicationName
        ? (string.IsNullOrWhiteSpace(ApplicationName) ? ProcessName : ApplicationName)
        : Title;

    public string SecondaryLabel => ListByApplicationName
        ? Title
        : $"{ProcessName}  ·  PID {ProcessId}";

    public string StateLabel => IsMinimized ? "Minimized" : string.Empty;

    public string PreferredMonitorNumber { get; init; } = string.Empty;

    public string PreferredMonitorToolTip =>
        string.IsNullOrEmpty(PreferredMonitorNumber)
            ? string.Empty
            : $"Always move to monitor {PreferredMonitorNumber}";
}
