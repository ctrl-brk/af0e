using Microsoft.Win32;

namespace RigCommander;

public sealed class MainForm : Form
{
    private const string StartupValueName = "RigCommander";

    private readonly IHost _app;
    private readonly RigCommanderSettings _settings;

    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _trayMenu;

    private readonly CheckBox _runAtStartupCheckBox;

    private bool _allowClose;
    private bool _shownOnce;

    public MainForm(IHost app, string serverUrl, RigCommanderSettings settings)
    {
        _app = app;
        _settings = settings;

        Text = "Rig Commander";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 560;
        Height = 300;
        MinimumSize = new Size(560, 300);

        var iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
        if (File.Exists(iconPath))
            Icon = new Icon(iconPath);

        var titleLabel = new Label
        {
            AutoSize = true,
            Left = 20,
            Top = 20,
            Font = new Font(Font.FontFamily, 12, FontStyle.Bold),
            Text = "Rig Commander"
        };

        var serverLabel = new Label
        {
            AutoSize = true,
            Left = 20,
            Top = 60,
            Text = $"Server: {serverUrl}\nRadio: {settings.ActiveProfile}"
        };

        _runAtStartupCheckBox = new CheckBox
        {
            Left = 20,
            Top = 160,
            Width = 180,
            Text = "Run at Windows startup",
            Checked = IsStartupEnabled()
        };
        _runAtStartupCheckBox.CheckedChanged += (_, _) => SetStartup(_runAtStartupCheckBox.Checked);

        var hideButton = new Button
        {
            Left = 20,
            Top = 200,
            Width = 120,
            Height = 32,
            Text = "Hide to Tray"
        };
        hideButton.Click += (_, _) => HideToTray();

        var exitButton = new Button
        {
            Left = 160,
            Top = 200,
            Width = 120,
            Height = 32,
            Text = "Exit"
        };
        exitButton.Click += async (_, _) => await ExitApplicationAsync();

        Controls.Add(titleLabel);
        Controls.Add(serverLabel);
        Controls.Add(_runAtStartupCheckBox);
        Controls.Add(hideButton);
        Controls.Add(exitButton);

        _trayMenu = new ContextMenuStrip();
        _trayMenu.Items.Add("Show", null, (_, _) => RestoreFromTray());
        _trayMenu.Items.Add("Exit", null, async (_, _) => await ExitApplicationAsync());

        _trayIcon = new NotifyIcon
        {
            Text = "Rig Commander",
            Visible = true,
            ContextMenuStrip = _trayMenu,
            Icon = Icon ?? SystemIcons.Application
        };
        _trayIcon.DoubleClick += (_, _) => RestoreFromTray();

        Resize += MainForm_Resize;
        FormClosing += MainForm_FormClosing;
        Shown += MainForm_Shown;
    }

    private void MainForm_Shown(object? sender, EventArgs e)
    {
        if (_shownOnce)
            return;

        _shownOnce = true;

        if (_settings.Ui.StartMinimized)
        {
            WindowState = FormWindowState.Minimized;
            HideToTray(showBalloon: false);
        }
    }

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        if (WindowState == FormWindowState.Minimized)
            HideToTray();
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_allowClose)
            return;

        e.Cancel = true;
        HideToTray();
    }

    private void HideToTray(bool showBalloon = true)
    {
        Hide();
        ShowInTaskbar = false;

        if (!showBalloon)
            return;

        /*
            _trayIcon.BalloonTipTitle = "Rig Commander";
            _trayIcon.BalloonTipText = "Still running in the system tray.";
            _trayIcon.ShowBalloonTip(1500);
        */
    }

    private void RestoreFromTray()
    {
        Show();
        ShowInTaskbar = true;
        WindowState = FormWindowState.Normal;
        Activate();
    }

    private async Task ExitApplicationAsync()
    {
        _allowClose = true;
        _trayIcon.Visible = false;

        try
        {
            await _app.StopAsync(TimeSpan.FromSeconds(5));
        }
        catch
        {
            // ignore shutdown errors
        }

        Close();
    }

    private static bool IsStartupEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
        return key?.GetValue(StartupValueName) is string;
    }

    private static void SetStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run",
            writable: true);

        if (key is null)
            return;

        if (enabled)
        {
            var exePath = Application.ExecutablePath;
            key.SetValue(StartupValueName, $"\"{exePath}\"");
        }
        else
        {
            key.DeleteValue(StartupValueName, false);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _trayIcon.Dispose();
            _trayMenu.Dispose();
        }

        base.Dispose(disposing);
    }
}
