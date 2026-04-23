using RigCommander.Abstractions;

namespace RigCommander.Services;

public sealed class NotifyIconTrayShell : ITrayShell
{
    private readonly Form _owner;
    private readonly Func<Task> _exitAsync;
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _trayMenu;

    public NotifyIconTrayShell(Form owner, string toolTipText, Icon? icon, Func<Task> exitAsync)
    {
        _owner = owner;
        _exitAsync = exitAsync;

        _trayMenu = new ContextMenuStrip();
        _trayMenu.Items.Add("Show", null, ShowMenuItem_Click);
        _trayMenu.Items.Add("Exit", null, ExitMenuItem_Click);

        _trayIcon = new NotifyIcon
        {
            Text = toolTipText,
            Visible = true,
            ContextMenuStrip = _trayMenu,
            Icon = icon ?? SystemIcons.Application
        };
        _trayIcon.DoubleClick += TrayIcon_DoubleClick;
    }

    public void HideToTray(bool showBalloon = true)
    {
        _owner.Hide();
        _owner.ShowInTaskbar = false;

        if (showBalloon)
            ShowBalloon("Rig Commander", "Still running in the system tray.", ToolTipIcon.Info, 1500);
    }

    public void ShowBalloon(string title, string text, ToolTipIcon icon = ToolTipIcon.None, int timeoutMs = 3000)
    {
        _trayIcon.BalloonTipTitle = title;
        _trayIcon.BalloonTipText = text;
        _trayIcon.BalloonTipIcon = icon;
        _trayIcon.ShowBalloonTip(timeoutMs);
    }

    public void RestoreFromTray()
    {
        _owner.Show();
        _owner.ShowInTaskbar = true;
        _owner.WindowState = FormWindowState.Normal;
        _owner.Activate();
    }

    public void SetVisible(bool visible)
    {
        _trayIcon.Visible = visible;
    }

    public void Dispose()
    {
        _trayIcon.DoubleClick -= TrayIcon_DoubleClick;
        _trayIcon.Dispose();
        _trayMenu.Dispose();
    }

    private void TrayIcon_DoubleClick(object? sender, EventArgs e)
    {
        RestoreFromTray();
    }

    private void ShowMenuItem_Click(object? sender, EventArgs e)
    {
        RestoreFromTray();
    }

    private async void ExitMenuItem_Click(object? sender, EventArgs e)
    {
        await _exitAsync();
    }
}
