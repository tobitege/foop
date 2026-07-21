using System.Reflection;
using System.Windows;
using Foop.Models;
using Foop.Services;

namespace Foop;

public partial class SettingsWindow : Window
{
    private readonly SettingsService _settingsService;

    internal SettingsWindow(SettingsService settingsService)
    {
        _settingsService = settingsService;
        InitializeComponent();

        var settings = _settingsService.Current;
        StartWithWindowsCheckBox.IsChecked = settings.StartWithWindows;
        StartMinimizedCheckBox.IsChecked = settings.StartMinimized;
        MinimizeToTrayCheckBox.IsChecked = settings.MinimizeToTray;
        CloseToTrayCheckBox.IsChecked = settings.CloseToTray;
        AutoMinimizeOnFoopingCheckBox.IsChecked = settings.AutoMinimizeOnFooping;
        ListByApplicationNameCheckBox.IsChecked = settings.ListByApplicationName;
        VersionText.Text = FormatVersionHint();
    }

    private static string FormatVersionHint()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version is null)
        {
            return "Foop";
        }

        return $"Foop v{version.Major}.{version.Minor}.{version.Build}";
    }

    private void OnCreateStartMenuIconClick(object sender, RoutedEventArgs e)
    {
        var scopeDialog = new StartMenuScopeWindow
        {
            Owner = this
        };
        if (scopeDialog.ShowDialog() != true || scopeDialog.SelectedScope is not StartMenuShortcutScope scope)
        {
            return;
        }

        if (scope == StartMenuShortcutScope.AllUsers && !SettingsService.CanCreateAllUsersShortcut())
        {
            var restart = MessageBox.Show(
                this,
                "Creating a Start menu shortcut for all users requires administrator rights.\n\nRestart Foop as administrator now?",
                "Foop",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (restart != MessageBoxResult.Yes)
            {
                return;
            }

            if (!SettingsService.TryRestartElevated(SettingsService.CreateAllUsersStartMenuArgument))
            {
                return;
            }

            Application.Current.Shutdown();
            return;
        }

        try
        {
            ShowShortcutCreatedMessage(scope, _settingsService.CreateStartMenuShortcut(scope));
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"The Start menu shortcut could not be created.\n\n{exception.Message}",
                "Foop",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    internal static void ShowShortcutCreatedMessage(StartMenuShortcutScope scope, string shortcutPath)
    {
        var scopeLabel = scope == StartMenuShortcutScope.AllUsers
            ? "all users"
            : "the current user";
        MessageBox.Show(
            $"Start menu shortcut created for {scopeLabel}.\n\n{shortcutPath}",
            "Foop",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var current = _settingsService.Current;
        var settings = new AppSettings
        {
            StartWithWindows = StartWithWindowsCheckBox.IsChecked == true,
            StartMinimized = StartMinimizedCheckBox.IsChecked == true,
            MinimizeToTray = MinimizeToTrayCheckBox.IsChecked == true,
            CloseToTray = CloseToTrayCheckBox.IsChecked == true,
            AutoMinimizeOnFooping = AutoMinimizeOnFoopingCheckBox.IsChecked == true,
            ListByApplicationName = ListByApplicationNameCheckBox.IsChecked == true,
            ViewMode = current.ViewMode,
            WindowWidth = current.WindowWidth,
            WindowHeight = current.WindowHeight,
            WindowLeft = current.WindowLeft,
            WindowTop = current.WindowTop,
            AutoMoveRules = current.AutoMoveRules.ToList()
        };

        try
        {
            _settingsService.Save(settings);
            DialogResult = true;
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                this,
                $"The settings could not be saved.\n\n{exception.Message}",
                "Foop",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => DialogResult = false;
}
