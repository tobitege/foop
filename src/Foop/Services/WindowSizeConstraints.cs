using Foop.Models;

namespace Foop.Services;

internal static class WindowSizeConstraints
{
    internal const double DefaultWidth = 760;
    internal const double DefaultHeight = 620;
    internal const double MinWidth = 580;
    internal const double MinHeight = 440;
    internal const double AbsoluteMax = 10_000;
    internal const double AbsoluteCoordinateMax = 100_000;

    internal static bool IsSafeStoredSize(double value) =>
        !double.IsNaN(value)
        && !double.IsInfinity(value)
        && value > 0
        && value <= AbsoluteMax;

    internal static bool IsSafeStoredCoordinate(double value) =>
        !double.IsNaN(value)
        && !double.IsInfinity(value)
        && value >= -AbsoluteCoordinateMax
        && value <= AbsoluteCoordinateMax;

    internal static (double Width, double Height) Resolve(
        double? storedWidth,
        double? storedHeight,
        ScreenRect workArea)
    {
        var width = storedWidth is double candidateWidth && IsSafeStoredSize(candidateWidth)
            ? candidateWidth
            : DefaultWidth;
        var height = storedHeight is double candidateHeight && IsSafeStoredSize(candidateHeight)
            ? candidateHeight
            : DefaultHeight;
        return ClampToMonitor(width, height, workArea);
    }

    internal static (double Width, double Height) ClampToMonitor(
        double width,
        double height,
        ScreenRect workArea)
    {
        var maxWidth = Math.Max(1, workArea.Width);
        var maxHeight = Math.Max(1, workArea.Height);
        var minWidth = Math.Min(MinWidth, maxWidth);
        var minHeight = Math.Min(MinHeight, maxHeight);

        if (!IsSafeStoredSize(width))
        {
            width = DefaultWidth;
        }

        if (!IsSafeStoredSize(height))
        {
            height = DefaultHeight;
        }

        width = Math.Clamp(width, minWidth, maxWidth);
        height = Math.Clamp(height, minHeight, maxHeight);
        return (width, height);
    }

    internal static bool WorkAreaContains(ScreenRect workArea, double x, double y) =>
        x >= workArea.X
        && y >= workArea.Y
        && x < workArea.X + workArea.Width
        && y < workArea.Y + workArea.Height;
}
