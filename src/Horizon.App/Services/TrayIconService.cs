using DrawingIcon = System.Drawing.Icon;
using Forms = System.Windows.Forms;

namespace Horizon.App.Services;

public sealed class TrayIconService : IDisposable
{
    private readonly Action _openAction;
    private readonly Forms.NotifyIcon _notifyIcon;
    private readonly Forms.ContextMenuStrip _contextMenu;
    private readonly DrawingIcon? _ownedIcon;
    private bool _disposed;

    public TrayIconService(Action openAction, Action exitAction)
    {
        _openAction = openAction;
        _contextMenu = new Forms.ContextMenuStrip();

        var openItem = new Forms.ToolStripMenuItem("打开 Horizon");
        openItem.Click += (_, _) => openAction();

        var exitItem = new Forms.ToolStripMenuItem("退出 Horizon");
        exitItem.Click += (_, _) => exitAction();

        _contextMenu.Items.Add(openItem);
        _contextMenu.Items.Add(new Forms.ToolStripSeparator());
        _contextMenu.Items.Add(exitItem);

        _ownedIcon = TryExtractExecutableIcon();
        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "Horizon",
            Icon = _ownedIcon ?? System.Drawing.SystemIcons.Application,
            ContextMenuStrip = _contextMenu,
            Visible = true
        };
        _notifyIcon.MouseClick += NotifyIcon_OnMouseClick;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _notifyIcon.Visible = false;
        _notifyIcon.MouseClick -= NotifyIcon_OnMouseClick;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
        _ownedIcon?.Dispose();
    }

    private void NotifyIcon_OnMouseClick(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button == Forms.MouseButtons.Left)
        {
            _openAction();
        }
    }

    private static DrawingIcon? TryExtractExecutableIcon()
    {
        try
        {
            var executablePath = Environment.ProcessPath;
            return string.IsNullOrWhiteSpace(executablePath)
                ? null
                : DrawingIcon.ExtractAssociatedIcon(executablePath);
        }
        catch
        {
            return null;
        }
    }
}
