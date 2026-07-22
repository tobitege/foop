using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace Foop.Services;

/// <summary>
/// Ensures one Foop process and lets a second launch ask the running instance to present a window.
/// </summary>
internal static class SingleInstanceService
{
    private const string MutexName = @"Local\Foop.2A7623D8-BC9E-4D68-AE56-83A84CB135EC";
    private const string ShowEventName = @"Local\Foop.ShowRequest.2A7623D8-BC9E-4D68-AE56-83A84CB135EC";

    private static Mutex? _mutex;
    private static EventWaitHandle? _showEvent;
    private static RegisteredWaitHandle? _registeredWait;

    internal static bool TryStartAsPrimaryInstance()
    {
        var mutex = new Mutex(true, MutexName, out var createdNew);
        if (!createdNew)
        {
            mutex.Dispose();
            SignalShowRequest();
            return false;
        }

        _mutex = mutex;
        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowEventName);
        return true;
    }

    internal static void WatchShowRequests(Action onShowRequested)
    {
        if (_showEvent is null)
        {
            return;
        }

        _registeredWait = ThreadPool.RegisterWaitForSingleObject(
            _showEvent,
            (_, _) =>
            {
                var dispatcher = Application.Current?.Dispatcher;
                if (dispatcher is null || dispatcher.HasShutdownStarted)
                {
                    return;
                }

                dispatcher.BeginInvoke(onShowRequested, DispatcherPriority.Normal);
            },
            state: null,
            millisecondsTimeOutInterval: Timeout.Infinite,
            executeOnlyOnce: false);
    }

    internal static void Release()
    {
        _registeredWait?.Unregister(null);
        _registeredWait = null;

        _showEvent?.Dispose();
        _showEvent = null;

        if (_mutex is null)
        {
            return;
        }

        try
        {
            _mutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
        }

        _mutex.Dispose();
        _mutex = null;
    }

    private static void SignalShowRequest()
    {
        try
        {
            using var showEvent = EventWaitHandle.OpenExisting(ShowEventName);
            showEvent.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
        }
    }
}
