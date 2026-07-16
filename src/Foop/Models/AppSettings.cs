namespace Foop.Models;

internal sealed class AppSettings
{
    public bool StartWithWindows { get; set; }

    public bool CreateStartMenuIcon { get; set; }

    public bool AutoMinimizeOnFooping { get; set; }

    public string ViewMode { get; set; } = AppViewModes.Detail;

    public double? WindowWidth { get; set; }

    public double? WindowHeight { get; set; }

    public double? WindowLeft { get; set; }

    public double? WindowTop { get; set; }

    public AppSettings Clone()
    {
        return new AppSettings
        {
            StartWithWindows = StartWithWindows,
            CreateStartMenuIcon = CreateStartMenuIcon,
            AutoMinimizeOnFooping = AutoMinimizeOnFooping,
            ViewMode = ViewMode,
            WindowWidth = WindowWidth,
            WindowHeight = WindowHeight,
            WindowLeft = WindowLeft,
            WindowTop = WindowTop
        };
    }
}
