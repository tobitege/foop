using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.ComponentModel;
using System.Windows.Interop;
using System.Windows.Threading;
using Foop.Models;
using Foop.Services;

namespace Foop;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private readonly DesktopWindowService _desktopWindowService;
    private readonly SettingsService _settingsService;
    private readonly Action _shutdownRequested;
    private MonitorDescriptor? _targetMonitor;
    private bool _allowClose;
    private bool _shutdownQueued;
    private bool _viewModeInitialized;
    private bool _persistWindowBounds;

    internal MainWindow(
        DesktopWindowService desktopWindowService,
        SettingsService settingsService,
        Action shutdownRequested)
    {
        _desktopWindowService = desktopWindowService;
        _settingsService = settingsService;
        _shutdownRequested = shutdownRequested;
        InitializeComponent();
        InitializeViewModes();
    }

    internal void InitializeForMonitor(MonitorDescriptor monitor, int activeMonitorCount)
    {
        _targetMonitor = monitor;
        Title = $"Foop · {monitor.DisplayName}";
        TargetMonitorText.Text = $"{monitor.DisplayName}  ·  {monitor.WorkArea.Width} × {monitor.WorkArea.Height}";
        MonitorCountText.Text = activeMonitorCount == 1
            ? "1 active monitor"
            : $"{activeMonitorCount} active monitors";
        ApplyStoredWindowSize(monitor);
        RefreshWindows();
        ShowActivated = false;
        Opacity = 0;
        WindowState = WindowState.Normal;
        Show();
        PlaceOnMonitor(monitor);
        Dispatcher.BeginInvoke(
            () =>
            {
                PlaceOnMonitor(monitor);
                WindowState = WindowState.Minimized;
                Opacity = 1;
                _persistWindowBounds = true;
            },
            DispatcherPriority.ContextIdle);
    }

    internal void CloseFromController()
    {
        _allowClose = true;
        Close();
    }

    private void InitializeViewModes()
    {
        var options = new[]
        {
            new ViewModeOption(ViewMode.Detail, "\uE8FD", "Detail"),
            new ViewModeOption(ViewMode.Grid, "\uE80A", "Grid")
        };
        ViewModeComboBox.ItemsSource = options;
        _viewModeInitialized = true;
        ViewModeComboBox.SelectedItem = AppViewModes.IsGrid(_settingsService.Current.ViewMode)
            ? options[1]
            : options[0];
    }

    private void ApplyStoredWindowSize(MonitorDescriptor monitor)
    {
        var dpi = GetDpiScale();
        var workAreaDip = ToDipWorkArea(monitor.WorkArea, dpi);
        var settings = _settingsService.Current;
        var storedWidth = ToDipSize(settings.WindowWidth, dpi.DpiScaleX);
        var storedHeight = ToDipSize(settings.WindowHeight, dpi.DpiScaleY);
        var (width, height) = WindowSizeConstraints.Resolve(storedWidth, storedHeight, workAreaDip);
        Width = width;
        Height = height;
        MaxWidth = Math.Max(MinWidth, workAreaDip.Width);
        MaxHeight = Math.Max(MinHeight, workAreaDip.Height);
    }

    private void RefreshWindows()
    {
        var selectedHandle = (WindowListBox.SelectedItem as DesktopWindow)?.Handle;
        var windows = _desktopWindowService.GetDesktopWindows();
        WindowListBox.ItemsSource = windows;
        WindowCountText.Text = windows.Count == 1 ? "1 window" : $"{windows.Count} windows";
        WindowListBox.SelectedItem = windows.FirstOrDefault(window => window.Handle == selectedHandle)
            ?? windows.FirstOrDefault();
        StatusText.Text = windows.Count == 0
            ? "No movable desktop windows found."
            : "Select a window.";
    }

    private void PlaceOnMonitor(MonitorDescriptor monitor)
    {
        var handle = new WindowInteropHelper(this).EnsureHandle();
        if (handle == nint.Zero)
        {
            return;
        }

        var settings = _settingsService.Current;
        if (WindowPlacementService.TryPlaceFromWorkAreaOffset(
                handle,
                monitor,
                settings.WindowLeft,
                settings.WindowTop,
                constrainToWorkArea: true))
        {
            return;
        }

        WindowPlacementService.CenterWindow(handle, monitor, constrainToWorkArea: true);
    }

    private void PerformFoop()
    {
        if (_targetMonitor is null || WindowListBox.SelectedItem is not DesktopWindow selectedWindow)
        {
            return;
        }

        var result = _desktopWindowService.MoveToMonitor(selectedWindow, _targetMonitor);
        StatusText.Text = result.Message;
        if (result.Success)
        {
            RefreshWindows();
            StatusText.Text = result.Message;
            if (result.Relocated && _settingsService.Current.AutoMinimizeOnFooping)
            {
                WindowState = WindowState.Minimized;
            }
        }
    }

    private void ApplyViewMode(ViewMode mode)
    {
        var items = WindowListBox.ItemsSource;
        var selected = WindowListBox.SelectedItem;
        WindowListBox.ItemsSource = null;

        if (mode == ViewMode.Grid)
        {
            WindowListBox.ItemTemplate = (DataTemplate)FindResource("GridItemTemplate");
            WindowListBox.ItemsPanel = (ItemsPanelTemplate)FindResource("GridItemsPanel");
            WindowListBox.ItemContainerStyle = (Style)FindResource("WindowGridItemStyle");
            VirtualizingPanel.SetIsVirtualizing(WindowListBox, false);
        }
        else
        {
            WindowListBox.ItemTemplate = (DataTemplate)FindResource("DetailItemTemplate");
            WindowListBox.ItemsPanel = (ItemsPanelTemplate)FindResource("DetailItemsPanel");
            WindowListBox.ItemContainerStyle = (Style)FindResource("WindowListItemStyle");
            VirtualizingPanel.SetIsVirtualizing(WindowListBox, true);
        }

        WindowListBox.ItemsSource = items;
        WindowListBox.SelectedItem = selected;
    }

    private void PersistCurrentWindowBounds()
    {
        if (!_persistWindowBounds || _targetMonitor is null)
        {
            return;
        }

        var handle = new WindowInteropHelper(this).Handle;
        if (handle == nint.Zero
            || !WindowPlacementService.TryGetNormalBounds(handle, out var normalBounds))
        {
            return;
        }

        var (width, height) = WindowSizeConstraints.ClampToMonitor(
            normalBounds.Width,
            normalBounds.Height,
            _targetMonitor.WorkArea);
        var placed = WindowPlacementService.PlaceWithin(
            new ScreenRect(normalBounds.X, normalBounds.Y, (int)width, (int)height),
            _targetMonitor.WorkArea);
        var offsetX = placed.X - _targetMonitor.WorkArea.X;
        var offsetY = placed.Y - _targetMonitor.WorkArea.Y;

        try
        {
            _settingsService.SaveWindowBounds(offsetX, offsetY, placed.Width, placed.Height);
        }
        catch
        {
            // Bounds persistence must not interrupt normal UI use.
        }
    }

    private DpiScale GetDpiScale()
    {
        try
        {
            return VisualTreeHelper.GetDpi(this);
        }
        catch
        {
            return new DpiScale(1, 1);
        }
    }

    private static ScreenRect ToDipWorkArea(ScreenRect workArea, DpiScale dpi)
    {
        var scaleX = dpi.DpiScaleX <= 0 ? 1 : dpi.DpiScaleX;
        var scaleY = dpi.DpiScaleY <= 0 ? 1 : dpi.DpiScaleY;
        return new ScreenRect(
            0,
            0,
            Math.Max(1, (int)Math.Round(workArea.Width / scaleX)),
            Math.Max(1, (int)Math.Round(workArea.Height / scaleY)));
    }

    private static double? ToDipSize(double? physicalSize, double dpiScale)
    {
        if (physicalSize is not double size || !WindowSizeConstraints.IsSafeStoredSize(size))
        {
            return null;
        }

        var scale = dpiScale <= 0 ? 1 : dpiScale;
        return size / scale;
    }

    private void OnFoopClick(object sender, RoutedEventArgs e) => PerformFoop();

    private void OnWindowListDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source)
        {
            return;
        }

        if (ItemsControl.ContainerFromElement(WindowListBox, source) is ListBoxItem
            && WindowListBox.SelectedItem is DesktopWindow)
        {
            PerformFoop();
        }
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshWindows();

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) =>
        FoopButton.IsEnabled = WindowListBox.SelectedItem is DesktopWindow;

    private void OnViewModeChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_viewModeInitialized || ViewModeComboBox.SelectedItem is not ViewModeOption option)
        {
            return;
        }

        ApplyViewMode(option.Mode);
        try
        {
            _settingsService.SaveViewMode(
                option.Mode == ViewMode.Grid ? AppViewModes.Grid : AppViewModes.Detail);
        }
        catch
        {
            // View persistence must not interrupt normal UI use.
        }
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsWindow(_settingsService)
        {
            Owner = this
        };
        dialog.ShowDialog();
    }

    private void OnRepoLinkRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        if (Opacity > 0 && WindowState != WindowState.Minimized)
        {
            RefreshWindows();
        }
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_allowClose || Application.Current.Dispatcher.HasShutdownStarted)
        {
            return;
        }

        PersistCurrentWindowBounds();
        e.Cancel = true;
        if (!_shutdownQueued)
        {
            _shutdownQueued = true;
            Dispatcher.BeginInvoke(_shutdownRequested, DispatcherPriority.Normal);
        }
    }

    private enum ViewMode
    {
        Detail,
        Grid
    }

    private sealed record ViewModeOption(ViewMode Mode, string IconGlyph, string Name);
}
