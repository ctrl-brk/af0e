using RigCommander.Abstractions;

namespace RigCommander.Presentation;

public sealed class MainFormPresenter(RigCommanderSettings settings, IStartupRegistration? startupRegistration)
{
    private bool _shownOnce;

    public MainFormViewState BuildInitialState(Uri serverUrl)
    {
        return new MainFormViewState(
            ServerLabelText: $"Server: {serverUrl}{Environment.NewLine}Radio: {settings.ActiveProfile}",
            RunAtStartupEnabled: startupRegistration?.IsEnabled() ?? false);
    }

    public bool ShouldStartMinimizedOnShown()
    {
        if (_shownOnce)
            return false;

        _shownOnce = true;
        return settings.Ui.StartMinimized;
    }

    public static bool ShouldHideToTrayOnResize(FormWindowState windowState)
    {
        return windowState == FormWindowState.Minimized;
    }

    public void OnRunAtStartupToggled(bool enabled, bool suppressChange, bool isDesignTime)
    {
        if (suppressChange || isDesignTime)
            return;

        startupRegistration?.SetEnabled(enabled);
    }
}
