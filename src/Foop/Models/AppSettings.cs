namespace Foop.Models;

internal sealed class AppSettings
{
    public bool StartWithWindows { get; set; }

    public bool AutoMinimizeOnFooping { get; set; }

    /// <summary>
    /// When true, Foop starts with the primary window hidden to the notification area.
    /// </summary>
    public bool StartMinimized { get; set; }

    /// <summary>
    /// When true, minimizing a Foop window hides it to the notification area.
    /// </summary>
    public bool MinimizeToTray { get; set; } = true;

    /// <summary>
    /// When true, closing a Foop window hides it to the notification area.
    /// </summary>
    public bool CloseToTray { get; set; } = true;

    /// <summary>
    /// When true, the window list shows the application name first and the window title second.
    /// </summary>
    public bool ListByApplicationName { get; set; } = true;

    public string ViewMode { get; set; } = AppViewModes.Detail;

    public double? WindowWidth { get; set; }

    public double? WindowHeight { get; set; }

    public double? WindowLeft { get; set; }

    public double? WindowTop { get; set; }

    public List<AutoMoveRule> AutoMoveRules { get; set; } = [];

    public AppSettings Clone()
    {
        return new AppSettings
        {
            StartWithWindows = StartWithWindows,
            AutoMinimizeOnFooping = AutoMinimizeOnFooping,
            StartMinimized = StartMinimized,
            MinimizeToTray = MinimizeToTray,
            CloseToTray = CloseToTray,
            ListByApplicationName = ListByApplicationName,
            ViewMode = ViewMode,
            WindowWidth = WindowWidth,
            WindowHeight = WindowHeight,
            WindowLeft = WindowLeft,
            WindowTop = WindowTop,
            AutoMoveRules = AutoMoveRules.ToList()
        };
    }
}
