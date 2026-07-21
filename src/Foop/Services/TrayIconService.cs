using System.Drawing;
using System.Windows.Forms;
using DrawingIcon = System.Drawing.Icon;

namespace Foop.Services;

internal sealed class TrayIconService : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly SettingsService _settingsService;
    private readonly Action _openPrimaryRequested;
    private readonly Action _exitRequested;
    private readonly ToolStripMenuItem _startWithWindowsItem;
    private readonly ToolStripMenuItem _startMinimizedItem;
    private readonly ToolStripMenuItem _minimizeToTrayItem;
    private readonly ToolStripMenuItem _closeToTrayItem;
    private bool _disposed;

    internal TrayIconService(
        SettingsService settingsService,
        Action openPrimaryRequested,
        Action exitRequested)
    {
        _settingsService = settingsService;
        _openPrimaryRequested = openPrimaryRequested;
        _exitRequested = exitRequested;

        _startWithWindowsItem = new ToolStripMenuItem("Start with Windows")
        {
            CheckOnClick = true
        };
        _startWithWindowsItem.Click += OnStartWithWindowsClick;

        _startMinimizedItem = new ToolStripMenuItem("Start minimized")
        {
            CheckOnClick = true
        };
        _startMinimizedItem.Click += OnStartMinimizedClick;

        _minimizeToTrayItem = new ToolStripMenuItem("Minimize to Tray")
        {
            CheckOnClick = true
        };
        _minimizeToTrayItem.Click += OnMinimizeToTrayClick;

        _closeToTrayItem = new ToolStripMenuItem("Close to Tray")
        {
            CheckOnClick = true
        };
        _closeToTrayItem.Click += OnCloseToTrayClick;

        var menu = new ContextMenuStrip();
        menu.Opening += OnMenuOpening;
        menu.Items.Add(_startWithWindowsItem);
        menu.Items.Add(_startMinimizedItem);
        menu.Items.Add(_minimizeToTrayItem);
        menu.Items.Add(_closeToTrayItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Open Foop", null, (_, _) => _openPrimaryRequested());
        menu.Items.Add("Exit Foop", null, (_, _) => _exitRequested());

        _notifyIcon = new NotifyIcon
        {
            Text = "Foop",
            Visible = true,
            Icon = LoadIcon(),
            ContextMenuStrip = menu
        };
        _notifyIcon.MouseClick += OnMouseClick;
        SyncMenuChecks();
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
    }

    private void OnMenuOpening(object? sender, System.ComponentModel.CancelEventArgs e) =>
        SyncMenuChecks();

    private void SyncMenuChecks()
    {
        var settings = _settingsService.Current;
        _startWithWindowsItem.Checked = settings.StartWithWindows;
        _startMinimizedItem.Checked = settings.StartMinimized;
        _minimizeToTrayItem.Checked = settings.MinimizeToTray;
        _closeToTrayItem.Checked = settings.CloseToTray;
    }

    private void OnStartWithWindowsClick(object? sender, EventArgs e)
    {
        try
        {
            _settingsService.SetStartWithWindows(_startWithWindowsItem.Checked);
        }
        catch
        {
            SyncMenuChecks();
        }
    }

    private void OnStartMinimizedClick(object? sender, EventArgs e)
    {
        try
        {
            _settingsService.SetStartMinimized(_startMinimizedItem.Checked);
        }
        catch
        {
            SyncMenuChecks();
        }
    }

    private void OnMinimizeToTrayClick(object? sender, EventArgs e)
    {
        try
        {
            _settingsService.SetMinimizeToTray(_minimizeToTrayItem.Checked);
        }
        catch
        {
            SyncMenuChecks();
        }
    }

    private void OnCloseToTrayClick(object? sender, EventArgs e)
    {
        try
        {
            _settingsService.SetCloseToTray(_closeToTrayItem.Checked);
        }
        catch
        {
            SyncMenuChecks();
        }
    }

    private void OnMouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _openPrimaryRequested();
        }
    }

    private static DrawingIcon LoadIcon()
    {
        var executablePath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            var associated = DrawingIcon.ExtractAssociatedIcon(executablePath);
            if (associated is not null)
            {
                return associated;
            }
        }

        return SystemIcons.Application;
    }
}
