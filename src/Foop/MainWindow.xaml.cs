using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using System.ComponentModel;
using System.Windows.Interop;
using System.Windows.Threading;
using Foop.Interop;
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
    private readonly IReadOnlyList<MonitorDescriptor> _activeMonitors;
    private readonly Action _shutdownRequested;
    private MonitorDescriptor? _targetMonitor;
    private HwndSource? _hwndSource;
    private bool _allowClose;
    private bool _shutdownQueued;
    private bool _viewModeInitialized;
    private bool _persistWindowBounds;
    private bool _constrainToTargetMonitor;
    private bool _startupPresentationComplete;

    internal MainWindow(
        DesktopWindowService desktopWindowService,
        SettingsService settingsService,
        IReadOnlyList<MonitorDescriptor> activeMonitors,
        Action shutdownRequested)
    {
        _desktopWindowService = desktopWindowService;
        _settingsService = settingsService;
        _activeMonitors = activeMonitors;
        _shutdownRequested = shutdownRequested;
        InitializeComponent();
        InitializeViewModes();
    }

    internal bool IsPrimaryTarget => _targetMonitor?.IsPrimary == true;

    internal bool TargetsMonitor(MonitorDescriptor monitor) =>
        _targetMonitor is not null && _targetMonitor.Handle == monitor.Handle;

    internal void InitializeForMonitor(MonitorDescriptor monitor, int activeMonitorCount)
    {
        _targetMonitor = monitor;
        Title = $"Foop · {monitor.DisplayName}";
        TargetMonitorText.Text = $"{monitor.DisplayName}  ·  {monitor.WorkArea.Width} × {monitor.WorkArea.Height}";
        MonitorCountText.Text = activeMonitorCount == 1
            ? "1 active monitor"
            : $"{activeMonitorCount} active monitors";

        // Primary monitor uses the tray instead of a taskbar button.
        ShowInTaskbar = !monitor.IsPrimary;
        RefreshWindows();
        ShowActivated = false;
        Opacity = 0;
        WindowState = WindowState.Normal;
        _constrainToTargetMonitor = false;

        // Park off-screen first so Show() cannot flash on the cursor's monitor.
        var handle = new WindowInteropHelper(this).EnsureHandle();
        if (handle != nint.Zero)
        {
            WindowPlacementService.ParkOffScreen(handle);
        }

        ApplyStoredWindowLayout(monitor, suppressRedraw: true);
        FinishStartupPresentation(monitor);
    }

    internal void Present()
    {
        if (_targetMonitor is null)
        {
            return;
        }

        Opacity = 0;
        ShowActivated = false;
        Show();
        WindowState = WindowState.Normal;
        ApplyStoredWindowLayout(_targetMonitor, suppressRedraw: true);
        Opacity = 1;
        _constrainToTargetMonitor = true;
        Activate();
    }

    internal void RestoreFromTray() => Present();

    internal void CloseFromController()
    {
        _allowClose = true;
        _constrainToTargetMonitor = false;
        Close();
    }

    protected override void OnClosed(EventArgs e)
    {
        if (_hwndSource is not null)
        {
            _hwndSource.RemoveHook(WndProc);
            _hwndSource = null;
        }

        base.OnClosed(e);
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

    private void FinishStartupPresentation(MonitorDescriptor monitor)
    {
        if (monitor.IsPrimary)
        {
            // Primary stays unshown in the tray until explicitly presented.
            Opacity = 1;
            _persistWindowBounds = true;
        }
        else
        {
            // Create the taskbar entry in a minimized state so no normal window can flash.
            WindowState = WindowState.Minimized;
            Show();
            Opacity = 1;
        }

        _constrainToTargetMonitor = true;
        _startupPresentationComplete = true;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        _hwndSource = HwndSource.FromHwnd(new WindowInteropHelper(this).Handle);
        _hwndSource?.AddHook(WndProc);
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (!_constrainToTargetMonitor || _targetMonitor is null || lParam == nint.Zero)
        {
            return nint.Zero;
        }

        if (msg == NativeMethods.WmMoving)
        {
            ConstrainMovingRect(lParam);
            handled = true;
            return nint.Zero;
        }

        if (msg == NativeMethods.WmWindowPosChanging)
        {
            ConstrainWindowPosChanging(hwnd, lParam);
            return nint.Zero;
        }

        return nint.Zero;
    }

    private void ConstrainMovingRect(nint lParam)
    {
        if (!TryGetTargetWorkArea(out var workArea))
        {
            return;
        }

        var rect = Marshal.PtrToStructure<NativeMethods.Rect>(lParam);
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        var clamped = WindowPlacementService.PlaceWithin(
            new ScreenRect(rect.Left, rect.Top, width, height),
            workArea);
        rect.Left = clamped.X;
        rect.Top = clamped.Y;
        rect.Right = clamped.X + clamped.Width;
        rect.Bottom = clamped.Y + clamped.Height;
        Marshal.StructureToPtr(rect, lParam, fDeleteOld: false);
    }

    private void ConstrainWindowPosChanging(nint hwnd, nint lParam)
    {
        if (!TryGetTargetWorkArea(out var workArea))
        {
            return;
        }

        var windowPos = Marshal.PtrToStructure<NativeMethods.WindowPos>(lParam);
        var noMove = (windowPos.Flags & NativeMethods.SwpNoMove) != 0;
        var noSize = (windowPos.Flags & NativeMethods.SwpNoSize) != 0;
        if (noMove && noSize)
        {
            return;
        }

        if (!NativeMethods.GetWindowRect(hwnd, out var currentRect))
        {
            return;
        }

        var x = noMove ? currentRect.Left : windowPos.X;
        var y = noMove ? currentRect.Top : windowPos.Y;
        var width = noSize ? currentRect.Right - currentRect.Left : windowPos.Cx;
        var height = noSize ? currentRect.Bottom - currentRect.Top : windowPos.Cy;
        var clamped = WindowPlacementService.PlaceWithin(
            new ScreenRect(x, y, width, height),
            workArea);

        if (clamped.X == x && clamped.Y == y && clamped.Width == width && clamped.Height == height)
        {
            return;
        }

        windowPos.X = clamped.X;
        windowPos.Y = clamped.Y;
        windowPos.Cx = clamped.Width;
        windowPos.Cy = clamped.Height;
        windowPos.Flags &= ~(NativeMethods.SwpNoMove | NativeMethods.SwpNoSize);
        Marshal.StructureToPtr(windowPos, lParam, fDeleteOld: false);
    }

    private bool TryGetTargetWorkArea(out ScreenRect workArea)
    {
        workArea = default;
        if (_targetMonitor is null)
        {
            return false;
        }

        var info = new NativeMethods.MonitorInfoEx
        {
            Size = Marshal.SizeOf<NativeMethods.MonitorInfoEx>()
        };
        if (NativeMethods.GetMonitorInfo(_targetMonitor.Handle, ref info))
        {
            workArea = new ScreenRect(
                info.WorkArea.Left,
                info.WorkArea.Top,
                info.WorkArea.Right - info.WorkArea.Left,
                info.WorkArea.Bottom - info.WorkArea.Top);
            return true;
        }

        workArea = _targetMonitor.WorkArea;
        return true;
    }

    private void ApplyStoredWindowLayout(MonitorDescriptor monitor, bool suppressRedraw = false)
    {
        var dpi = GetDpiScale();
        var scaleX = dpi.DpiScaleX <= 0 ? 1 : dpi.DpiScaleX;
        var scaleY = dpi.DpiScaleY <= 0 ? 1 : dpi.DpiScaleY;
        var workAreaDip = ToDipWorkArea(monitor.WorkArea, dpi);
        var settings = _settingsService.Current;
        var storedWidth = NormalizeStoredDipSize(settings.WindowWidth, scaleX, workAreaDip.Width);
        var storedHeight = NormalizeStoredDipSize(settings.WindowHeight, scaleY, workAreaDip.Height);
        var (widthDip, heightDip) = WindowSizeConstraints.Resolve(storedWidth, storedHeight, workAreaDip);
        var widthPhysical = (int)Math.Round(widthDip * scaleX);
        var heightPhysical = (int)Math.Round(heightDip * scaleY);

        ScreenRect target;
        if (settings.WindowLeft is double offsetX
            && settings.WindowTop is double offsetY
            && WindowSizeConstraints.IsSafeStoredCoordinate(offsetX)
            && WindowSizeConstraints.IsSafeStoredCoordinate(offsetY))
        {
            target = WindowPlacementService.PlaceWithin(
                new ScreenRect(
                    monitor.WorkArea.X + (int)Math.Round(offsetX),
                    monitor.WorkArea.Y + (int)Math.Round(offsetY),
                    widthPhysical,
                    heightPhysical),
                monitor.WorkArea);
        }
        else
        {
            target = WindowPlacementService.CenterWithin(
                new ScreenRect(0, 0, widthPhysical, heightPhysical),
                monitor.WorkArea,
                constrainToWorkArea: true);
        }

        // Keep WPF and Win32 coordinates aligned so Show() does not jump to another monitor.
        MaxWidth = Math.Max(MinWidth, workAreaDip.Width);
        MaxHeight = Math.Max(MinHeight, workAreaDip.Height);
        Width = widthDip;
        Height = heightDip;
        Left = target.X / scaleX;
        Top = target.Y / scaleY;

        var handle = new WindowInteropHelper(this).EnsureHandle();
        if (handle == nint.Zero)
        {
            return;
        }

        if (!WindowPlacementService.TryPlaceFromWorkAreaOffset(
                handle,
                monitor,
                settings.WindowLeft,
                settings.WindowTop,
                widthPhysical,
                heightPhysical,
                constrainToWorkArea: true,
                suppressRedraw))
        {
            WindowPlacementService.PlaceWithSize(
                handle,
                monitor,
                widthPhysical,
                heightPhysical,
                centerWhenNoOffset: true,
                suppressRedraw);
        }
    }

    private void RefreshWindows()
    {
        var selectedHandle = (WindowListBox.SelectedItem as DesktopWindow)?.Handle;
        var windows = _desktopWindowService.GetDesktopWindows(
                _settingsService.Current.ListByApplicationName)
            .Select(ApplyMonitorPreference)
            .ToArray();
        WindowListBox.ItemsSource = windows;
        WindowCountText.Text = windows.Length == 1 ? "1 window" : $"{windows.Length} windows";
        // Keep a previous selection only when that window still exists; never auto-pick another app.
        WindowListBox.SelectedItem = selectedHandle is nint handle
            ? windows.FirstOrDefault(window => window.Handle == handle)
            : null;
        UpdateFoopButtonEnabled();
        StatusText.Text = windows.Length == 0
            ? "No movable desktop windows found."
            : WindowListBox.SelectedItem is DesktopWindow
                ? "Ready to Foop the selected window."
                : "Select a window.";
    }

    private DesktopWindow ApplyMonitorPreference(DesktopWindow window)
    {
        var rule = _settingsService.FindAutoMoveRule(window);
        return rule is null
            ? window
            : window with
            {
                PreferredMonitorNumber =
                    MonitorDescriptor.GetDisplayNumber(rule.MonitorDeviceName)
            };
    }

    private void UpdateFoopButtonEnabled() =>
        FoopButton.IsEnabled = WindowListBox.SelectedItem is DesktopWindow;

    private void PerformFoop()
    {
        if (_targetMonitor is null || WindowListBox.SelectedItem is not DesktopWindow selectedWindow)
        {
            return;
        }

        MoveWindowToMonitor(selectedWindow, _targetMonitor);
    }

    private void MoveWindowToMonitor(DesktopWindow selectedWindow, MonitorDescriptor targetMonitor)
    {
        var result = _desktopWindowService.MoveToMonitor(selectedWindow, targetMonitor);
        StatusText.Text = result.Message;
        if (result.Success)
        {
            RefreshWindows();
            StatusText.Text = result.Message;
            if (result.Relocated && _settingsService.Current.AutoMinimizeOnFooping)
            {
                if (_settingsService.Current.MinimizeToTray)
                {
                    Hide();
                }
                else
                {
                    WindowState = WindowState.Minimized;
                }
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

        var dpi = GetDpiScale();
        var scaleX = dpi.DpiScaleX <= 0 ? 1 : dpi.DpiScaleX;
        var scaleY = dpi.DpiScaleY <= 0 ? 1 : dpi.DpiScaleY;
        var workAreaDip = ToDipWorkArea(_targetMonitor.WorkArea, dpi);

        var (widthPhysical, heightPhysical) = WindowSizeConstraints.ClampToMonitor(
            normalBounds.Width,
            normalBounds.Height,
            _targetMonitor.WorkArea);
        var placed = WindowPlacementService.PlaceWithin(
            new ScreenRect(normalBounds.X, normalBounds.Y, (int)widthPhysical, (int)heightPhysical),
            _targetMonitor.WorkArea);

        // Persist size in DIP so restore matches WPF Width/Height without a second conversion.
        var widthDip = placed.Width / scaleX;
        var heightDip = placed.Height / scaleY;
        (widthDip, heightDip) = WindowSizeConstraints.ClampToMonitor(widthDip, heightDip, workAreaDip);

        var offsetX = placed.X - _targetMonitor.WorkArea.X;
        var offsetY = placed.Y - _targetMonitor.WorkArea.Y;

        try
        {
            _settingsService.SaveWindowBounds(offsetX, offsetY, widthDip, heightDip);
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

    private static double? NormalizeStoredDipSize(double? storedSize, double dpiScale, int workAreaDipSize)
    {
        if (storedSize is not double size || !WindowSizeConstraints.IsSafeStoredSize(size))
        {
            return null;
        }

        var scale = dpiScale <= 0 ? 1 : dpiScale;
        // Legacy values were stored as physical pixels; convert once when they exceed the DIP work area.
        if (scale > 1.01 && size > workAreaDipSize)
        {
            size /= scale;
        }

        return WindowSizeConstraints.IsSafeStoredSize(size) ? size : null;
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

    private void OnWindowListRightClick(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is not DependencyObject source
            || ItemsControl.ContainerFromElement(WindowListBox, source) is not ListBoxItem item
            || item.DataContext is not DesktopWindow desktopWindow)
        {
            return;
        }

        e.Handled = true;
        WindowListBox.SelectedItem = desktopWindow;
        var dialog = new AppActionWindow(desktopWindow, _activeMonitors, _settingsService)
        {
            Owner = this
        };
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        RefreshWindows();
        if (dialog.SendTarget is MonitorDescriptor targetMonitor)
        {
            MoveWindowToMonitor(desktopWindow, targetMonitor);
        }
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshWindows();

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateFoopButtonEnabled();
        if (WindowListBox.SelectedItem is DesktopWindow)
        {
            StatusText.Text = "Ready to Foop the selected window.";
        }
        else if (WindowListBox.Items.Count > 0)
        {
            StatusText.Text = "Select a window.";
        }
    }

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
        if (dialog.ShowDialog() == true)
        {
            RefreshWindows();
        }
    }

    private void OnRepoLinkRequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }

    private void OnActivated(object? sender, EventArgs e)
    {
        if (Opacity > 0 && WindowState != WindowState.Minimized && IsVisible)
        {
            RefreshWindows();
        }
    }

    private void OnStateChanged(object? sender, EventArgs e)
    {
        if (_startupPresentationComplete
            && WindowState == WindowState.Minimized
            && _settingsService.Current.MinimizeToTray)
        {
            Hide();
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

        if (_settingsService.Current.CloseToTray)
        {
            Hide();
            return;
        }

        // Otherwise closing any Foop window exits the application.
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
