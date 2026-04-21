namespace RigCommander.Abstractions;

public interface ITrayShell : IDisposable
{
    void HideToTray(bool showBalloon = true);
    void RestoreFromTray();
    void SetVisible(bool visible);
}
