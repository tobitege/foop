namespace Foop.Models;

internal static class AppViewModes
{
    internal const string Detail = "Detail";
    internal const string Grid = "Grid";

    internal static string Normalize(string? value) =>
        string.Equals(value, Grid, StringComparison.OrdinalIgnoreCase) ? Grid : Detail;

    internal static bool IsGrid(string? value) =>
        string.Equals(Normalize(value), Grid, StringComparison.Ordinal);
}
