using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using Foop.Models;
using Foop.Services;

namespace Foop;

public partial class AppActionWindow : Window
{
    private readonly DesktopWindow _desktopWindow;
    private readonly SettingsService _settingsService;
    private readonly IReadOnlyList<MonitorDescriptor> _monitors;

    internal AppActionWindow(
        DesktopWindow desktopWindow,
        IReadOnlyList<MonitorDescriptor> monitors,
        SettingsService settingsService)
    {
        _desktopWindow = desktopWindow;
        _monitors = monitors;
        _settingsService = settingsService;
        InitializeComponent();

        MaxHeight = Math.Max(360, SystemParameters.WorkArea.Height - 48);
        AppInitialText.Text = desktopWindow.Initial;
        AppNameText.Text = desktopWindow.PrimaryLabel;
        AppDetailText.Text = desktopWindow.SecondaryLabel;
        PopulateMonitorButtons();
    }

    internal MonitorDescriptor? SendTarget { get; private set; }

    private void PopulateMonitorButtons()
    {
        var currentRule = _settingsService.FindAutoMoveRule(_desktopWindow);
        foreach (var monitor in _monitors)
        {
            SendButtonsPanel.Children.Add(CreateMonitorButton(
                monitor,
                isSelected: false,
                "Send to",
                OnSendToMonitorClick));
            AlwaysMoveButtonsPanel.Children.Add(CreateMonitorButton(
                monitor,
                currentRule is not null
                    && string.Equals(
                        currentRule.MonitorDeviceName,
                        monitor.DeviceName,
                        StringComparison.OrdinalIgnoreCase),
                "Always move to",
                OnAlwaysMoveToMonitorClick));
        }

        RemoveLastButtonMargin(SendButtonsPanel);
        RemoveLastButtonMargin(AlwaysMoveButtonsPanel);
        DisableRuleButton.IsEnabled = currentRule is not null;
    }

    private Button CreateMonitorButton(
        MonitorDescriptor monitor,
        bool isSelected,
        string actionName,
        RoutedEventHandler clickHandler)
    {
        var button = new Button
        {
            Tag = monitor,
            Style = (Style)FindResource("MonitorButtonStyle"),
            Content = CreateMonitorButtonContent(monitor, isSelected)
        };
        AutomationProperties.SetName(button, $"{actionName} {monitor.DisplayName}");
        if (isSelected)
        {
            button.Background = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(37, 99, 235));
            button.BorderBrush = new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(191, 219, 254));
        }

        button.Click += clickHandler;
        return button;
    }

    private static StackPanel CreateMonitorButtonContent(
        MonitorDescriptor monitor,
        bool isSelected)
    {
        var panel = new StackPanel
        {
            Orientation = Orientation.Horizontal
        };
        panel.Children.Add(new TextBlock
        {
            FontFamily = new System.Windows.Media.FontFamily("Segoe Fluent Icons"),
            FontSize = 15,
            Text = "\uE7F4",
            VerticalAlignment = VerticalAlignment.Center
        });
        panel.Children.Add(new TextBlock
        {
            Margin = new Thickness(10, 0, 0, 0),
            Text = isSelected ? $"{monitor.DisplayName}  ·  Current" : monitor.DisplayName,
            VerticalAlignment = VerticalAlignment.Center
        });
        return panel;
    }

    private static void RemoveLastButtonMargin(StackPanel panel)
    {
        if (panel.Children.Count > 0 && panel.Children[^1] is Button button)
        {
            button.Margin = new Thickness(0);
        }
    }

    private void OnSendToMonitorClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: MonitorDescriptor monitor })
        {
            return;
        }

        SendTarget = monitor;
        DialogResult = true;
    }

    private void OnAlwaysMoveToMonitorClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: MonitorDescriptor monitor })
        {
            return;
        }

        try
        {
            _settingsService.SetAutoMoveRule(_desktopWindow, monitor);
            DialogResult = true;
        }
        catch (Exception exception)
        {
            ShowSaveError(exception);
        }
    }

    private void OnDisableRuleClick(object sender, RoutedEventArgs e)
    {
        try
        {
            _settingsService.ClearAutoMoveRule(_desktopWindow);
            DialogResult = true;
        }
        catch (Exception exception)
        {
            ShowSaveError(exception);
        }
    }

    private void ShowSaveError(Exception exception)
    {
        MessageBox.Show(
            this,
            $"The auto-move rule could not be saved.\n\n{exception.Message}",
            "Foop",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
