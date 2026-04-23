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
    private readonly ActivationIdStore? _activationIdStore;

    private bool _allowClose;
    private bool _suppressStartupToggle;

    // ReSharper disable ConvertToAutoProperty
    public RichTextBox LogBox => _logBox;
    public RichTextBox ScriptLogBox => _scriptLogBox;
    // ReSharper restore ConvertToAutoProperty

    public void ShowErrorBalloon(string message)
    {
        if (_trayShell is null || !IsHandleCreated)
            return;

        BeginInvoke(() =>
        {
            var firstLine = message
                .Replace("\r", string.Empty, StringComparison.Ordinal)
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(firstLine))
                firstLine = "A warning/error was logged.";

            if (firstLine.Length > 180)
                firstLine = firstLine[..180] + "...";

            _trayShell.ShowBalloon("Error", firstLine, ToolTipIcon.Error, 5000);
        });
    }

    public MainForm()
        : this(
            serverUrl: new Uri("http://localhost:5050"),
            settings: new RigCommanderSettings { ActiveProfile = "Design-time profile" },
            hostShutdownService: null,
            startupRegistration: null,
            activationIdStore: null,
            isDesignTime: true)
    {
    }

    public MainForm(IHost app, Uri serverUrl, RigCommanderSettings settings)
        : this(
            serverUrl,
            settings,
            new HostShutdownService(app),
            new WindowsStartupRegistration("RigCommander"),
            activationIdStore: null,
            IsDesignerHosted())
    {
    }

    public MainForm(IHost app, Uri serverUrl, RigCommanderSettings settings, ActivationIdStore activationIdStore)
        : this(
            serverUrl,
            settings,
            new HostShutdownService(app),
            new WindowsStartupRegistration("RigCommander"),
            activationIdStore,
            IsDesignerHosted())
    {
    }

    private MainForm(
        Uri serverUrl,
        RigCommanderSettings settings,
        IHostShutdownService? hostShutdownService,
        IStartupRegistration? startupRegistration,
        ActivationIdStore? activationIdStore,
        bool isDesignTime)
    {
        _hostShutdownService = hostShutdownService;
        _activationIdStore = activationIdStore;
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

        _activationIdTextBox.TextChanged += ActivationIdTextBox_TextChanged;
        _clearActivationIdButton.Click += ClearActivationIdButton_Click;
        UpdateActivationIdStateFromText();
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
    private void ClearScriptLogButton_Click(object? sender, EventArgs e)
    {
        ScriptLogBox.Clear();
    }
    private void ClearLogButton_Click(object? sender, EventArgs e)
    {
        LogBox.Clear();
    }

    private void ActivationIdTextBox_TextChanged(object? sender, EventArgs e)
    {
        UpdateActivationIdStateFromText();
    }

    private void ClearActivationIdButton_Click(object? sender, EventArgs e)
    {
        _activationIdTextBox.Text = string.Empty;
    }

    private void UpdateActivationIdStateFromText()
    {
        var raw = _activationIdTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(raw))
        {
            _activationIdStore?.Set(null);
            _activationIdTextBox.BackColor = SystemColors.Window;
            return;
        }

        if (int.TryParse(raw, out var parsed) && parsed > 0)
        {
            _activationIdStore?.Set(parsed);
            _activationIdTextBox.BackColor = SystemColors.Window;
            return;
        }

        _activationIdStore?.Set(null);
        _activationIdTextBox.BackColor = Color.MistyRose;
    }
}
