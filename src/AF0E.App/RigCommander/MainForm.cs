using System.ComponentModel;
using RigCommander.Abstractions;
using RigCommander.Presentation;
using RigCommander.Services;

namespace RigCommander;

public sealed partial class MainForm : Form
{
    private readonly MainFormPresenter _presenter;
    private readonly IHostShutdownService? _hostShutdownService;
    private readonly NotifyIconTrayShell? _trayShell;

    private bool _allowClose;
    private bool _suppressStartupToggle;

    public RichTextBox LogBox => _logBox;
    public RichTextBox ScriptLogBox => _scriptLogBox;

    public MainForm()
        : this(
            serverUrl: new Uri("http://localhost:5050"),
            settings: new RigCommanderSettings { ActiveProfile = "Design-time profile" },
            hostShutdownService: null,
            startupRegistration: null,
            isDesignTime: true)
    {
    }

    public MainForm(IHost app, Uri serverUrl, RigCommanderSettings settings)
        : this(
            serverUrl,
            settings,
            new HostShutdownService(app),
            new WindowsStartupRegistration("RigCommander"),
            IsDesignerHosted())
    {
    }

    private MainForm(
        Uri serverUrl,
        RigCommanderSettings settings,
        IHostShutdownService? hostShutdownService,
        IStartupRegistration? startupRegistration,
        bool isDesignTime)
    {
        _hostShutdownService = hostShutdownService;
        _presenter = new MainFormPresenter(settings, startupRegistration);

        InitializeComponent();

        ApplyRuntimeState(serverUrl, isDesignTime);

        _trayShell = isDesignTime
            ? null
            : new NotifyIconTrayShell(this, "Rig Commander", Icon ?? SystemIcons.Application, ExitApplicationAsync);
    }

    private static bool IsDesignerHosted() => LicenseManager.UsageMode == LicenseUsageMode.Designtime;

    private void ApplyRuntimeState(Uri serverUrl, bool isDesignTime)
    {
        var state = _presenter.BuildInitialState(serverUrl);
        _serverLabel.Text = state.ServerLabelText;

        var iconPath = Path.Combine(AppContext.BaseDirectory, "app.ico");
        if (File.Exists(iconPath))
            Icon = new Icon(iconPath);

        if (isDesignTime)
            return;

        _suppressStartupToggle = true;
        _runAtStartupCheckBox.Checked = state.RunAtStartupEnabled;
        _suppressStartupToggle = false;
    }

    private void MainForm_Shown(object? sender, EventArgs e)
    {
        if (!_presenter.ShouldStartMinimizedOnShown())
            return;

        WindowState = FormWindowState.Minimized;
        _trayShell?.HideToTray(showBalloon: false);
    }

    private void MainForm_Resize(object? sender, EventArgs e)
    {
        if (MainFormPresenter.ShouldHideToTrayOnResize(WindowState))
            _trayShell?.HideToTray();
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_allowClose)
            return;

        e.Cancel = true;
        _trayShell?.HideToTray();
    }

    private async Task ExitApplicationAsync()
    {
        _allowClose = true;
        _trayShell?.SetVisible(false);

        try
        {
            if (_hostShutdownService is not null)
                await _hostShutdownService.StopAsync(TimeSpan.FromSeconds(5));
        }
        catch
        {
            // ignore shutdown errors
        }

        Close();
    }

    private void RunAtStartupCheckBox_CheckedChanged(object? sender, EventArgs e)
    {
        _presenter.OnRunAtStartupToggled(_runAtStartupCheckBox.Checked, _suppressStartupToggle, IsDesignerHosted());
    }

    private void HideButton_Click(object? sender, EventArgs e)
    {
        _trayShell?.HideToTray();
    }

    private async void ExitButton_Click(object? sender, EventArgs e)
    {
        await ExitApplicationAsync();
    }
}
