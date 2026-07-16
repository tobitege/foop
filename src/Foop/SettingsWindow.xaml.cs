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
        CreateStartMenuIconCheckBox.IsChecked = settings.CreateStartMenuIcon;
        AutoMinimizeOnFoopingCheckBox.IsChecked = settings.AutoMinimizeOnFooping;
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        var current = _settingsService.Current;
        var settings = new AppSettings
        {
            StartWithWindows = StartWithWindowsCheckBox.IsChecked == true,
            CreateStartMenuIcon = CreateStartMenuIconCheckBox.IsChecked == true,
            AutoMinimizeOnFooping = AutoMinimizeOnFoopingCheckBox.IsChecked == true,
            ViewMode = current.ViewMode,
            WindowWidth = current.WindowWidth,
            WindowHeight = current.WindowHeight,
            WindowLeft = current.WindowLeft,
            WindowTop = current.WindowTop
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
