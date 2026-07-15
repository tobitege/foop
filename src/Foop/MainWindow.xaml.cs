using System.Text;
using System.Windows;
using System.Windows.Controls;
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
    private readonly Action _shutdownRequested;
    private MonitorDescriptor? _targetMonitor;
    private bool _allowClose;
    private bool _shutdownQueued;

    internal MainWindow(DesktopWindowService desktopWindowService, Action shutdownRequested)
    {
        _desktopWindowService = desktopWindowService;
        _shutdownRequested = shutdownRequested;
        InitializeComponent();
    }

    internal void InitializeForMonitor(MonitorDescriptor monitor, int activeMonitorCount)
    {
        _targetMonitor = monitor;
        Title = $"Foop · {monitor.DisplayName}";
        TargetMonitorText.Text = $"{monitor.DisplayName}  ·  {monitor.WorkArea.Width} × {monitor.WorkArea.Height}";
        MonitorCountText.Text = activeMonitorCount == 1
            ? "1 aktiver Monitor"
            : $"{activeMonitorCount} aktive Monitore";
        RefreshWindows();
        ShowActivated = false;
        Opacity = 0;
        WindowState = WindowState.Normal;
        Show();
        MoveToMonitor(monitor);
        Dispatcher.BeginInvoke(
            () =>
            {
                MoveToMonitor(monitor);
                WindowState = WindowState.Minimized;
                Opacity = 1;
            },
            DispatcherPriority.ContextIdle);
    }

    internal void CloseFromController()
    {
        _allowClose = true;
        Close();
    }

    private void RefreshWindows()
    {
        var selectedHandle = (WindowListBox.SelectedItem as DesktopWindow)?.Handle;
        var windows = _desktopWindowService.GetDesktopWindows();
        WindowListBox.ItemsSource = windows;
        WindowCountText.Text = windows.Count == 1 ? "1 Fenster" : $"{windows.Count} Fenster";
        WindowListBox.SelectedItem = windows.FirstOrDefault(window => window.Handle == selectedHandle)
            ?? windows.FirstOrDefault();
        StatusText.Text = windows.Count == 0
            ? "Keine verschiebbaren Desktopfenster gefunden."
            : "Wähle ein Fenster aus.";
    }

    private void MoveToMonitor(MonitorDescriptor monitor)
    {
        var handle = new WindowInteropHelper(this).Handle;
        if (handle != nint.Zero)
        {
            WindowPlacementService.CenterWindow(handle, monitor, constrainToWorkArea: true);
        }
    }

    private void OnFoopClick(object sender, RoutedEventArgs e)
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
        }
    }

    private void OnRefreshClick(object sender, RoutedEventArgs e) => RefreshWindows();

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) =>
        FoopButton.IsEnabled = WindowListBox.SelectedItem is DesktopWindow;

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

        e.Cancel = true;
        if (!_shutdownQueued)
        {
            _shutdownQueued = true;
            Dispatcher.BeginInvoke(_shutdownRequested, DispatcherPriority.Normal);
        }
    }
}
