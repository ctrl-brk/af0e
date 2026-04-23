using RigCommander.Abstractions;

namespace RigCommander.Presentation;

public sealed class MainFormPresenter(RigCommanderSettings settings, IStartupRegistration? startupRegistration)
{
    private bool _shownOnce;

    public MainFormViewState BuildInitialState(Uri serverUrl)
    {
        var listening = BuildListeningLabel(settings);

        return new MainFormViewState(
            ServerLabelText: $"Server: {serverUrl}{Environment.NewLine}Radio: {settings.ActiveProfile}{Environment.NewLine}Forwarding: {listening}",
            RunAtStartupEnabled: startupRegistration?.IsEnabled() ?? false);
    }

    private static string BuildListeningLabel(RigCommanderSettings settings)
    {
        if (!settings.AdifUdp.Enabled)
            return "disabled";

        if (settings.AdifUdp.JoinMulticastGroup)
            return $"{settings.AdifUdp.MulticastGroup}:{settings.AdifUdp.Port}";

        return $"0.0.0.0:{settings.AdifUdp.Port}";
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
