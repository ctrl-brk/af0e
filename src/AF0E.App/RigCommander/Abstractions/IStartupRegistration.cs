namespace RigCommander.Abstractions;

public interface IStartupRegistration
{
    bool IsEnabled();
    void SetEnabled(bool enabled);
}

