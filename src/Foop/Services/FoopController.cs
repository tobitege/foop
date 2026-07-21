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
    private TrayIconService? _trayIcon;
    private AutoMoveService? _autoMoveService;
    private bool _displayRefreshPending;
    private bool _isShuttingDown;

    internal void Start()
    {
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        RebuildTaskbarWindows();
        _autoMoveService = new AutoMoveService(
            _desktopWindowService,
            _settingsService,
            () => _activeMonitors,
            Application.Current.Dispatcher);
        _trayIcon = new TrayIconService(_settingsService, OpenWindowAtCursor, Shutdown);
        Application.Current.Dispatcher.BeginInvoke(
            PresentLaunchWindow,
            DispatcherPriority.ApplicationIdle);
        Application.Current.Dispatcher.BeginInvoke(
            StartAutoMoveService,
            DispatcherPriority.ApplicationIdle);
        ProcessPendingStartupActions();
    }

    private void StartAutoMoveService()
    {
        if (!_isShuttingDown)
        {
            _autoMoveService?.Start(TimeSpan.FromSeconds(1));
        }
    }

    private void ProcessPendingStartupActions()
    {
        if (!SettingsService.HasCreateAllUsersStartMenuArgument(Environment.GetCommandLineArgs())
            || !SettingsService.CanCreateAllUsersShortcut())
        {
            return;
        }

        Application.Current.Dispatcher.BeginInvoke(
            ConfirmAndCreateAllUsersShortcut,
            DispatcherPriority.ApplicationIdle);
    }

    private void ConfirmAndCreateAllUsersShortcut()
    {
        if (_isShuttingDown)
        {
            return;
        }

        OpenWindowAtCursor();

        var create = MessageBox.Show(
            "Foop is now running as administrator.\n\nCreate the Start menu shortcut for all users now?",
            "Foop",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (create != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            var shortcutPath = _settingsService.CreateStartMenuShortcut(StartMenuShortcutScope.AllUsers);
            SettingsWindow.ShowShortcutCreatedMessage(StartMenuShortcutScope.AllUsers, shortcutPath);
        }
        catch (Exception exception)
        {
            MessageBox.Show(
                $"The Start menu shortcut could not be created.\n\n{exception.Message}",
                "Foop",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    public void Dispose()
    {
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        _autoMoveService?.Dispose();
        _autoMoveService = null;
        _trayIcon?.Dispose();
        _trayIcon = null;
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
            var monitorWindow = new MainWindow(
                _desktopWindowService,
                _settingsService,
                _activeMonitors,
                Shutdown);
            _monitorWindows.Add(monitorWindow);
            monitorWindow.InitializeForMonitor(monitor, _activeMonitors.Count);
        }
    }

    private void PresentLaunchWindow()
    {
        if (_isShuttingDown)
        {
            return;
        }

        // Start minimized applies only to Windows logon autostart, not Start-menu launches.
        if (_settingsService.Current.StartMinimized
            && SettingsService.HasAutostartArgument(Environment.GetCommandLineArgs()))
        {
            return;
        }

        OpenWindowAtCursor();
    }

    private void OpenWindowAtCursor()
    {
        if (_isShuttingDown || _activeMonitors.Count == 0)
        {
            return;
        }

        var launchMonitor = _monitorService.GetMonitorAtCursor(_activeMonitors);
        OpenWindowForMonitor(launchMonitor);
    }

    private void OpenWindowForMonitor(MonitorDescriptor monitor)
    {
        var window = _monitorWindows.FirstOrDefault(candidate => candidate.TargetsMonitor(monitor))
            ?? _monitorWindows.FirstOrDefault(candidate => candidate.IsPrimaryTarget)
            ?? _monitorWindows.FirstOrDefault();
        window?.Present();
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
        _autoMoveService?.Dispose();
        _autoMoveService = null;
        _trayIcon?.Dispose();
        _trayIcon = null;
        foreach (var monitorWindow in _monitorWindows)
        {
            monitorWindow.CloseFromController();
        }

        _monitorWindows.Clear();
        Application.Current.Shutdown();
    }
}
