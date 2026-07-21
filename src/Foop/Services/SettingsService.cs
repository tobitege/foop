using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Principal;
using System.Text.Json;
using Foop.Models;
using Microsoft.Win32;

namespace Foop.Services;

internal sealed class SettingsService
{
    internal const string CreateAllUsersStartMenuArgument = "--create-start-menu-all-users";

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
            var minimizeToTray = ReadBool(
                document.RootElement,
                "MinimizeToTray",
                defaultValue: true);
            return new AppSettings
            {
                StartWithWindows = ReadBool(document.RootElement, "StartWithWindows"),
                AutoMinimizeOnFooping = ReadBool(document.RootElement, "AutoMinimizeOnFooping"),
                StartMinimized = ReadBool(document.RootElement, "StartMinimized"),
                MinimizeToTray = minimizeToTray,
                CloseToTray = ReadBool(
                    document.RootElement,
                    "CloseToTray",
                    defaultValue: minimizeToTray),
                ListByApplicationName = ReadBool(
                    document.RootElement,
                    "ListByApplicationName",
                    defaultValue: true),
                ViewMode = ReadViewMode(document.RootElement, "ViewMode"),
                WindowWidth = ReadSafeSize(document.RootElement, "WindowWidth"),
                WindowHeight = ReadSafeSize(document.RootElement, "WindowHeight"),
                WindowLeft = ReadSafeCoordinate(document.RootElement, "WindowLeft"),
                WindowTop = ReadSafeCoordinate(document.RootElement, "WindowTop"),
                AutoMoveRules = ReadAutoMoveRules(document.RootElement, "AutoMoveRules")
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
            settings.AutoMinimizeOnFooping,
            settings.StartMinimized,
            settings.MinimizeToTray,
            settings.CloseToTray,
            settings.ListByApplicationName,
            ViewMode = AppViewModes.Normalize(settings.ViewMode),
            WindowWidth = SanitizeStoredSize(settings.WindowWidth),
            WindowHeight = SanitizeStoredSize(settings.WindowHeight),
            WindowLeft = SanitizeStoredCoordinate(settings.WindowLeft),
            WindowTop = SanitizeStoredCoordinate(settings.WindowTop),
            AutoMoveRules = SanitizeAutoMoveRules(settings.AutoMoveRules)
        };
        File.WriteAllText(_settingsPath, JsonSerializer.Serialize(payload, SerializerOptions));
        Current = settings.Clone();
        Current.StartMinimized = payload.StartMinimized;
        Current.MinimizeToTray = payload.MinimizeToTray;
        Current.CloseToTray = payload.CloseToTray;
        Current.ListByApplicationName = payload.ListByApplicationName;
        Current.ViewMode = payload.ViewMode;
        Current.WindowWidth = payload.WindowWidth;
        Current.WindowHeight = payload.WindowHeight;
        Current.WindowLeft = payload.WindowLeft;
        Current.WindowTop = payload.WindowTop;
        Current.AutoMoveRules = payload.AutoMoveRules;
        if (applyIntegrations)
        {
            ApplyStartWithWindows(Current.StartWithWindows);
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

    internal void SetStartWithWindows(bool enabled)
    {
        var settings = Current.Clone();
        settings.StartWithWindows = enabled;
        Save(settings, applyIntegrations: true);
    }

    internal void SetMinimizeToTray(bool enabled)
    {
        var settings = Current.Clone();
        settings.MinimizeToTray = enabled;
        Save(settings, applyIntegrations: false);
    }

    internal void SetCloseToTray(bool enabled)
    {
        var settings = Current.Clone();
        settings.CloseToTray = enabled;
        Save(settings, applyIntegrations: false);
    }

    internal void SetStartMinimized(bool enabled)
    {
        var settings = Current.Clone();
        settings.StartMinimized = enabled;
        Save(settings, applyIntegrations: false);
    }

    internal AutoMoveRule? FindAutoMoveRule(DesktopWindow window) =>
        Current.AutoMoveRules.FirstOrDefault(rule => DesktopWindowIdentity.Matches(rule, window));

    internal void SetAutoMoveRule(DesktopWindow window, MonitorDescriptor monitor)
    {
        var settings = Current.Clone();
        settings.AutoMoveRules.RemoveAll(rule => DesktopWindowIdentity.Matches(rule, window));
        settings.AutoMoveRules.Add(new AutoMoveRule(
            window.ExecutablePath,
            window.ProcessName,
            monitor.DeviceName));
        Save(settings, applyIntegrations: false);
    }

    internal void ClearAutoMoveRule(DesktopWindow window)
    {
        var settings = Current.Clone();
        settings.AutoMoveRules.RemoveAll(rule => DesktopWindowIdentity.Matches(rule, window));
        Save(settings, applyIntegrations: false);
    }

    internal static bool CanCreateAllUsersShortcut() => IsCurrentProcessElevated();

    internal static bool HasCreateAllUsersStartMenuArgument(IEnumerable<string>? args) =>
        args is not null
        && args.Any(argument => string.Equals(
            argument,
            CreateAllUsersStartMenuArgument,
            StringComparison.OrdinalIgnoreCase));

    internal static bool TryRestartElevated(string arguments)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = GetExecutablePath(),
                Arguments = arguments,
                UseShellExecute = true,
                Verb = "runas"
            });
            return true;
        }
        catch (Win32Exception exception) when (exception.NativeErrorCode == 1223)
        {
            return false;
        }
    }

    internal string CreateStartMenuShortcut(StartMenuShortcutScope scope)
    {
        var programsFolder = scope == StartMenuShortcutScope.AllUsers
            ? Environment.GetFolderPath(Environment.SpecialFolder.CommonPrograms)
            : Environment.GetFolderPath(Environment.SpecialFolder.Programs);
        if (string.IsNullOrWhiteSpace(programsFolder))
        {
            throw new InvalidOperationException("The Start menu folder could not be determined.");
        }

        if (scope == StartMenuShortcutScope.AllUsers && !CanCreateAllUsersShortcut())
        {
            throw new InvalidOperationException(
                "Creating the shortcut for all users requires administrator rights. Restart Foop as administrator and try again.");
        }

        var shortcutPath = Path.Combine(programsFolder, ShortcutFileName);
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

        return shortcutPath;
    }

    private static double? SanitizeStoredSize(double? value) =>
        value is double size && WindowSizeConstraints.IsSafeStoredSize(size) ? size : null;

    private static double? SanitizeStoredCoordinate(double? value) =>
        value is double coordinate && WindowSizeConstraints.IsSafeStoredCoordinate(coordinate)
            ? coordinate
            : null;

    private static List<AutoMoveRule> SanitizeAutoMoveRules(IEnumerable<AutoMoveRule>? rules)
    {
        var result = new List<AutoMoveRule>();
        if (rules is null)
        {
            return result;
        }

        foreach (var rule in rules.Take(256))
        {
            var executablePath = SanitizeString(rule.ExecutablePath, 32_767);
            var processName = SanitizeString(rule.ProcessName, 260);
            var monitorDeviceName = SanitizeString(rule.MonitorDeviceName, 260);
            if ((string.IsNullOrEmpty(executablePath) && string.IsNullOrEmpty(processName))
                || string.IsNullOrEmpty(monitorDeviceName))
            {
                continue;
            }

            var sanitized = new AutoMoveRule(executablePath, processName, monitorDeviceName);
            result.RemoveAll(existing => RulesReferToSameApplication(existing, sanitized));
            result.Add(sanitized);
        }

        return result;
    }

    private static List<AutoMoveRule> ReadAutoMoveRules(JsonElement root, string propertyName)
    {
        try
        {
            if (!root.TryGetProperty(propertyName, out var property)
                || property.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            var rules = new List<AutoMoveRule>();
            foreach (var element in property.EnumerateArray().Take(256))
            {
                if (element.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                rules.Add(new AutoMoveRule(
                    ReadString(element, "ExecutablePath", 32_767),
                    ReadString(element, "ProcessName", 260),
                    ReadString(element, "MonitorDeviceName", 260)));
            }

            return SanitizeAutoMoveRules(rules);
        }
        catch
        {
            return [];
        }
    }

    private static string ReadString(JsonElement root, string propertyName, int maxLength)
    {
        if (!root.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String)
        {
            return string.Empty;
        }

        return SanitizeString(property.GetString(), maxLength);
    }

    private static string SanitizeString(string? value, int maxLength)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        return trimmed.Length <= maxLength ? trimmed : string.Empty;
    }

    private static bool RulesReferToSameApplication(AutoMoveRule left, AutoMoveRule right)
    {
        if (!string.IsNullOrWhiteSpace(left.ExecutablePath)
            && !string.IsNullOrWhiteSpace(right.ExecutablePath))
        {
            return string.Equals(
                left.ExecutablePath,
                right.ExecutablePath,
                StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(
            left.ProcessName,
            right.ProcessName,
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool ReadBool(JsonElement root, string propertyName, bool defaultValue = false)
    {
        try
        {
            if (!root.TryGetProperty(propertyName, out var property))
            {
                return defaultValue;
            }

            return property.ValueKind switch
            {
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.String => bool.TryParse(property.GetString(), out var parsed) && parsed,
                JsonValueKind.Number => property.TryGetInt64(out var number) && number != 0,
                _ => defaultValue
            };
        }
        catch
        {
            return defaultValue;
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

    private static bool IsCurrentProcessElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
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
