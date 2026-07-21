using System.Windows.Threading;
using Foop.Models;

namespace Foop.Services;

internal sealed class AutoMoveService : IDisposable
{
    private readonly DesktopWindowService _desktopWindowService;
    private readonly SettingsService _settingsService;
    private readonly Func<IReadOnlyList<MonitorDescriptor>> _getActiveMonitors;
    private readonly DispatcherTimer _timer;
    private HashSet<string> _activeApplications = new(StringComparer.OrdinalIgnoreCase);
    private bool _initialScanPending;
    private bool _isChecking;

    internal AutoMoveService(
        DesktopWindowService desktopWindowService,
        SettingsService settingsService,
        Func<IReadOnlyList<MonitorDescriptor>> getActiveMonitors,
        Dispatcher dispatcher)
    {
        _desktopWindowService = desktopWindowService;
        _settingsService = settingsService;
        _getActiveMonitors = getActiveMonitors;
        _timer = new DispatcherTimer(
            TimeSpan.FromMilliseconds(500),
            DispatcherPriority.Background,
            OnTimerTick,
            dispatcher);
    }

    internal void Start(TimeSpan startupDelay)
    {
        if (startupDelay > TimeSpan.Zero)
        {
            _initialScanPending = true;
            _timer.Interval = startupDelay;
            _timer.Start();
            return;
        }

        InitializeActiveApplications();
        _timer.Start();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (_isChecking)
        {
            return;
        }

        _isChecking = true;
        try
        {
            if (_initialScanPending)
            {
                _initialScanPending = false;
                InitializeActiveApplications();
                _timer.Interval = TimeSpan.FromMilliseconds(500);
                return;
            }

            CheckForNewApplications();
        }
        catch
        {
            // Window enumeration and process metadata can change while being inspected.
        }
        finally
        {
            _isChecking = false;
        }
    }

    private void InitializeActiveApplications()
    {
        try
        {
            _activeApplications = GetCurrentApplicationKeys();
        }
        catch
        {
            _activeApplications.Clear();
        }
    }

    private void CheckForNewApplications()
    {
        var windows = _desktopWindowService.GetDesktopWindows(
            _settingsService.Current.ListByApplicationName);
        var currentGroups = windows
            .GroupBy(DesktopWindowIdentity.GetKey, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var currentApplications = currentGroups
            .Select(group => group.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var firstWindow in FindNewFirstInstances(currentGroups, _activeApplications))
        {
            var rule = _settingsService.FindAutoMoveRule(firstWindow);
            if (rule is null)
            {
                continue;
            }

            var monitor = _getActiveMonitors().FirstOrDefault(candidate =>
                string.Equals(
                    candidate.DeviceName,
                    rule.MonitorDeviceName,
                    StringComparison.OrdinalIgnoreCase));
            if (monitor is not null)
            {
                _desktopWindowService.MoveToMonitor(firstWindow, monitor);
            }
        }

        _activeApplications = currentApplications;
    }

    internal static IReadOnlyList<DesktopWindow> FindNewFirstInstances(
        IEnumerable<IGrouping<string, DesktopWindow>> currentGroups,
        ISet<string> activeApplications) =>
        currentGroups
            .Where(group => !activeApplications.Contains(group.Key))
            .Select(group => group
                .OrderBy(window => window.ProcessId)
                .ThenBy(window => window.Handle)
                .First())
            .ToArray();

    private HashSet<string> GetCurrentApplicationKeys() =>
        _desktopWindowService
            .GetDesktopWindows(_settingsService.Current.ListByApplicationName)
            .Select(DesktopWindowIdentity.GetKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
