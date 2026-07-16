using System.Globalization;
using System.IO;
using System.Text.Json;
using Foop.Models;
using Microsoft.Win32;

namespace Foop.Services;

internal sealed class SettingsService
{
    private const string RunValueName = "Foop";
    private const string ShortcutFileName = "Foop.lnk";
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _settingsDirectory;
    private readonly string _settingsPath;

    internal SettingsService()
    {
        _settingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Foop");
        _settingsPath = Path.Combine(_settingsDirectory, "settings.json");
        Current = Load();
    }

    internal AppSettings Current { get; private set; }

    internal AppSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
            {
                return new AppSettings();
            }

            using var document = JsonDocument.Parse(File.ReadAllText(_settingsPath));
            return new AppSettings
            {
                StartWithWindows = ReadBool(document.RootElement, "StartWithWindows"),
                CreateStartMenuIcon = ReadBool(document.RootElement, "CreateStartMenuIcon"),
                AutoMinimizeOnFooping = ReadBool(document.RootElement, "AutoMinimizeOnFooping"),
                ViewMode = ReadViewMode(document.RootElement, "ViewMode"),
                WindowWidth = ReadSafeSize(document.RootElement, "WindowWidth"),
                WindowHeight = ReadSafeSize(document.RootElement, "WindowHeight"),
                WindowLeft = ReadSafeCoordinate(document.RootElement, "WindowLeft"),
                WindowTop = ReadSafeCoordinate(document.RootElement, "WindowTop")
            };
        }
        catch
        {
            return new AppSettings();
        }
    }

    internal void Save(AppSettings settings, bool applyIntegrations = true)
    {
        ArgumentNullException.ThrowIfNull(settings);

        Directory.CreateDirectory(_settingsDirectory);
        var payload = new
        {
            settings.StartWithWindows,
            settings.CreateStartMenuIcon,
            settings.AutoMinimizeOnFooping,
            ViewMode = AppViewModes.Normalize(settings.ViewMode),
            WindowWidth = SanitizeStoredSize(settings.WindowWidth),
            WindowHeight = SanitizeStoredSize(settings.WindowHeight),
            WindowLeft = SanitizeStoredCoordinate(settings.WindowLeft),
            WindowTop = SanitizeStoredCoordinate(settings.WindowTop)
        };
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(payload, SerializerOptions));
        Current = settings.Clone();
        Current.ViewMode = payload.ViewMode;
        Current.WindowWidth = payload.WindowWidth;
        Current.WindowHeight = payload.WindowHeight;
        Current.WindowLeft = payload.WindowLeft;
        Current.WindowTop = payload.WindowTop;
        if (applyIntegrations)
        {
            ApplyIntegrations(Current);
        }
    }

    internal void SaveWindowBounds(double left, double top, double width, double height)
    {
        var settings = Current.Clone();
        settings.WindowLeft = left;
        settings.WindowTop = top;
        settings.WindowWidth = width;
        settings.WindowHeight = height;
        Save(settings, applyIntegrations: false);
    }

    internal void SaveViewMode(string viewMode)
    {
        var settings = Current.Clone();
        settings.ViewMode = AppViewModes.Normalize(viewMode);
        Save(settings, applyIntegrations: false);
    }

    internal void ApplyIntegrations(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ApplyStartWithWindows(settings.StartWithWindows);
        ApplyStartMenuShortcut(settings.CreateStartMenuIcon);
    }

    private static double? SanitizeStoredSize(double? value) =>
        value is double size && WindowSizeConstraints.IsSafeStoredSize(size) ? size : null;

    private static double? SanitizeStoredCoordinate(double? value) =>
        value is double coordinate && WindowSizeConstraints.IsSafeStoredCoordinate(coordinate)
            ? coordinate
            : null;

    private static bool ReadBool(JsonElement root, string propertyName)
    {
        try
        {
            if (!root.TryGetProperty(propertyName, out var property))
            {
                return false;
            }

            return property.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => bool.TryParse(property.GetString(), out var parsed) && parsed,
                JsonValueKind.Number => property.TryGetInt64(out var number) && number != 0,
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    private static string ReadViewMode(JsonElement root, string propertyName)
    {
        try
        {
            if (!root.TryGetProperty(propertyName, out var property)
                || property.ValueKind != JsonValueKind.String)
            {
                return AppViewModes.Detail;
            }

            return AppViewModes.Normalize(property.GetString());
        }
        catch
        {
            return AppViewModes.Detail;
        }
    }

    private static double? ReadSafeSize(JsonElement root, string propertyName) =>
        ReadSafeDouble(root, propertyName, WindowSizeConstraints.IsSafeStoredSize);

    private static double? ReadSafeCoordinate(JsonElement root, string propertyName) =>
        ReadSafeDouble(root, propertyName, WindowSizeConstraints.IsSafeStoredCoordinate);

    private static double? ReadSafeDouble(
        JsonElement root,
        string propertyName,
        Func<double, bool> isSafe)
    {
        try
        {
            if (!root.TryGetProperty(propertyName, out var property))
            {
                return null;
            }

            double value;
            switch (property.ValueKind)
            {
                case JsonValueKind.Number:
                    if (!property.TryGetDouble(out value))
                    {
                        return null;
                    }

                    break;
                case JsonValueKind.String:
                    if (!double.TryParse(
                            property.GetString(),
                            NumberStyles.Float,
                            CultureInfo.InvariantCulture,
                            out value))
                    {
                        return null;
                    }

                    break;
                default:
                    return null;
            }

            return isSafe(value) ? value : null;
        }
        catch
        {
            return null;
        }
    }

    private static void ApplyStartWithWindows(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            writable: true);
        if (key is null)
        {
            throw new InvalidOperationException("The startup registry key could not be opened.");
        }

        if (enabled)
        {
            var executablePath = GetExecutablePath();
            key.SetValue(RunValueName, QuotePath(executablePath));
            return;
        }

        key.DeleteValue(RunValueName, throwOnMissingValue: false);
    }

    private static void ApplyStartMenuShortcut(bool enabled)
    {
        var programsFolder = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        if (string.IsNullOrWhiteSpace(programsFolder))
        {
            throw new InvalidOperationException("The Start menu folder could not be determined.");
        }

        var shortcutPath = Path.Combine(programsFolder, ShortcutFileName);
        if (!enabled)
        {
            if (File.Exists(shortcutPath))
            {
                File.Delete(shortcutPath);
            }

            return;
        }

        var executablePath = GetExecutablePath();
        var workingDirectory = Path.GetDirectoryName(executablePath)
            ?? throw new InvalidOperationException("Foop's working directory could not be determined.");

        var shellType = Type.GetTypeFromProgID("WScript.Shell")
            ?? throw new InvalidOperationException("WScript.Shell is not available on this system.");
        dynamic shell = Activator.CreateInstance(shellType)
            ?? throw new InvalidOperationException("WScript.Shell could not be created.");
        try
        {
            var shortcut = shell.CreateShortcut(shortcutPath);
            try
            {
                shortcut.TargetPath = executablePath;
                shortcut.WorkingDirectory = workingDirectory;
                shortcut.Description = "Foop – bring windows onto the current monitor";
                shortcut.IconLocation = $"{executablePath},0";
                shortcut.Save();
            }
            finally
            {
                if (shortcut is not null)
                {
                    System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shortcut);
                }
            }
        }
        finally
        {
            System.Runtime.InteropServices.Marshal.FinalReleaseComObject(shell);
        }
    }

    private static string GetExecutablePath()
    {
        var path = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            throw new InvalidOperationException("The Foop executable path could not be determined.");
        }

        return path;
    }

    private static string QuotePath(string path) =>
        path.Contains(' ', StringComparison.Ordinal) ? $"\"{path}\"" : path;
}
