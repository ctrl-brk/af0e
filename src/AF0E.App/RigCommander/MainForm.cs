using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using RigCommander.Abstractions;
using RigCommander.Presentation;
using RigCommander.Services;

namespace RigCommander;

[SuppressMessage("ReSharper", "AsyncVoidEventHandlerMethod")]
public sealed partial class MainForm : Form
{
    private readonly MainFormPresenter _presenter;
    private readonly IHostShutdownService? _hostShutdownService;
    private readonly NotifyIconTrayShell? _trayShell;
    private readonly ActivationIdStore? _activationIdStore;
    private readonly ActivationIdValidationService? _activationIdValidationService;

    private bool _allowClose;
    private bool _suppressStartupToggle;
    private bool _isApplyingActivationId;
    private int? _appliedActivationId;
    private string? _appliedActivationDisplayText;

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
        _activationIdValidationService = isDesignTime || !settings.AdifUdp.Forwarding.Enabled
            ? null
            : new ActivationIdValidationService(settings.LogbookApiUrl!, settings.AdifUdp.Forwarding);
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

        _activationIdLabel.Enabled = state.ActivationIdInputEnabled;
        _activationIdTextBox.Enabled = state.ActivationIdInputEnabled;
        _setActivationIdButton.Enabled = state.ActivationIdInputEnabled;
        _clearActivationIdButton.Enabled = state.ActivationIdInputEnabled;

        if (!state.ActivationIdInputEnabled)
        {
            _appliedActivationId = null;
            _appliedActivationDisplayText = null;
            _activationIdStore?.Set(null);
            _activationIdTextBox.Text = string.Empty;
            _activationIdTextBox.BackColor = SystemColors.Control;
            _setActivationIdButton.Enabled = false;
            _clearActivationIdButton.Enabled = false;
            UpdateActivationInfoLabel();
            return;
        }

        _appliedActivationId = _activationIdStore?.Get();
        _activationIdTextBox.Text = _appliedActivationId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        _activationIdTextBox.TextChanged += ActivationIdTextBox_TextChanged;
        _setActivationIdButton.Click += SetActivationIdButton_Click;
        _clearActivationIdButton.Click += ClearActivationIdButton_Click;
        UpdateActivationIdInputState();
    }

    private void MainForm_Shown(object? sender, EventArgs e)
    {
        if (!_presenter.ShouldStartMinimizedOnShown())
            return;

        WindowState = FormWindowState.Minimized;
        _trayShell?.HideToTray(true);
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
        UpdateActivationIdInputState();
    }

    private async void SetActivationIdButton_Click(object? sender, EventArgs e)
    {
        if (!_activationIdTextBox.Enabled || _isApplyingActivationId)
            return;

        var raw = _activationIdTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(raw))
        {
            ApplyActivation(null);
            return;
        }

        if (!TryParseActivationId(raw, out var activationId))
        {
            _activationIdTextBox.BackColor = Color.MistyRose;
            MessageBox.Show(this, "ActivationId must be a positive integer.", "Invalid ActivationId", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            UpdateActivationIdInputState();
            return;
        }

        if (_activationIdValidationService is null)
        {
            MessageBox.Show(this, "Activation validation is unavailable because the Logbook API endpoint is not configured.", "ActivationId Validation Unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            _isApplyingActivationId = true;
            UseWaitCursor = true;
            UpdateActivationIdInputState();

            var validation = await _activationIdValidationService.ValidateAsync(activationId);

            if (validation is null)
            {
                _activationIdTextBox.BackColor = Color.MistyRose;
                MessageBox.Show(this, $"Invalid ActivationId {activationId}", "Activation Not Found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ApplyActivation(validation.Id, validation.ParkNum, validation.ParkName);
        }
        catch (Exception ex)
        {
            _activationIdTextBox.BackColor = Color.MistyRose;
            MessageBox.Show(this, $"Could not load Activation from Logbook API.{Environment.NewLine}{Environment.NewLine}{ex.Message}", "ActivationId Lookup Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            _isApplyingActivationId = false;
            UseWaitCursor = false;
            UpdateActivationIdInputState();
        }
    }

    private void ClearActivationIdButton_Click(object? sender, EventArgs e)
    {
        if (!_activationIdTextBox.Enabled || _isApplyingActivationId)
            return;

        ApplyActivation(null);
    }

    private void ApplyActivation(int? activationId, string? parkNum = null, string? parkName = null)
    {
        _activationIdStore?.Set(activationId);
        _appliedActivationId = activationId;
        _appliedActivationDisplayText = activationId is null
            ? null
            : BuildActivationDisplayText(parkNum, parkName);
        _activationIdTextBox.Text = activationId?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        UpdateActivationInfoLabel();
        UpdateActivationIdInputState();
    }

    private void UpdateActivationInfoLabel()
    {
        var hasActivationInfo = !string.IsNullOrWhiteSpace(_appliedActivationDisplayText);
        _activationInfoLabel.Text = hasActivationInfo ? _appliedActivationDisplayText : " ";
        _activationInfoLabel.ForeColor = hasActivationInfo ? SystemColors.ControlText : SystemColors.GrayText;
    }

    private void UpdateActivationIdInputState()
    {
        var inputEnabled = _activationIdTextBox.Enabled;

        if (!inputEnabled)
        {
            _activationIdTextBox.BackColor = SystemColors.Control;
            _setActivationIdButton.Enabled = false;
            _clearActivationIdButton.Enabled = false;
            return;
        }

        var raw = _activationIdTextBox.Text.Trim();
        var parsedActivationId = TryParseActivationId(raw, out var parsedValue)
            ? parsedValue
            : (int?)null;
        var canParse = string.IsNullOrWhiteSpace(raw) || parsedActivationId.HasValue;
        var hasPendingChange = !string.IsNullOrWhiteSpace(raw)
            ? !canParse || parsedActivationId != _appliedActivationId
            : _appliedActivationId.HasValue;

        _setActivationIdButton.Enabled = !_isApplyingActivationId && hasPendingChange && canParse;
        _clearActivationIdButton.Enabled = !_isApplyingActivationId && (_appliedActivationId.HasValue || raw.Length > 0);

        if (_isApplyingActivationId)
        {
            _activationIdTextBox.BackColor = Color.LemonChiffon;
            return;
        }

        if (string.IsNullOrWhiteSpace(raw))
        {
            _activationIdTextBox.BackColor = SystemColors.Window;
            return;
        }

        _activationIdTextBox.BackColor = canParse && !hasPendingChange
            ? SystemColors.Window
            : canParse
                ? Color.LemonChiffon
                : Color.MistyRose;
    }

    private static bool TryParseActivationId(string raw, out int activationId)
        => int.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out activationId) && activationId > 0;

    private static string BuildActivationDisplayText(string? parkNum, string? parkName)
    {
        if (string.IsNullOrWhiteSpace(parkNum))
            return string.IsNullOrWhiteSpace(parkName) ? string.Empty : parkName.Trim();

        return string.IsNullOrWhiteSpace(parkName)
            ? parkNum.Trim()
            : $"{parkNum.Trim()} - {parkName.Trim()}";
    }
}
