namespace RigCommander.Abstractions;

public interface ITrayShell : IDisposable
{
    void HideToTray(bool showBalloon = true);
    void RestoreFromTray();
    void SetVisible(bool visible);
    void ShowBalloon(string title, string text, ToolTipIcon icon = ToolTipIcon.None, int timeoutMs = 5000);
}
