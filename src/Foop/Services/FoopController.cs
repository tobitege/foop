using System.Windows;
using System.Windows.Threading;
using Foop.Models;
using Microsoft.Win32;

namespace Foop.Services;

internal sealed class FoopController : IDisposable
{
    private readonly MonitorService _monitorService = new();
    private readonly DesktopWindowService _desktopWindowService = new();
    private readonly SettingsService _settingsService = new();
    private readonly List<MainWindow> _monitorWindows = [];
    private IReadOnlyList<MonitorDescriptor> _activeMonitors = [];
    private bool _displayRefreshPending;
    private bool _isShuttingDown;

    internal void Start()
    {
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        RebuildTaskbarWindows();
    }

    public void Dispose()
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
    }

    private void RebuildTaskbarWindows()
    {
        _displayRefreshPending = false;
        foreach (var monitorWindow in _monitorWindows)
        {
            monitorWindow.CloseFromController();
        }

        _monitorWindows.Clear();
        _activeMonitors = _monitorService.GetActiveMonitors();
        foreach (var monitor in _activeMonitors)
        {
            var monitorWindow = new MainWindow(_desktopWindowService, _settingsService, Shutdown);
            _monitorWindows.Add(monitorWindow);
            monitorWindow.InitializeForMonitor(monitor, _activeMonitors.Count);
        }
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        var dispatcher = Application.Current.Dispatcher;
        if (_displayRefreshPending || dispatcher.HasShutdownStarted)
        {
            return;
        }

        _displayRefreshPending = true;
        dispatcher.BeginInvoke(RebuildTaskbarWindows, DispatcherPriority.ContextIdle);
    }

    private void Shutdown()
    {
        if (_isShuttingDown)
        {
            return;
        }

        _isShuttingDown = true;
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        foreach (var monitorWindow in _monitorWindows)
        {
            monitorWindow.CloseFromController();
        }

        _monitorWindows.Clear();
        Application.Current.Shutdown();
    }
}
