using RigCommander.Abstractions;

namespace RigCommander.Tests.TestDoubles;

internal sealed class StartupRegistrationSpy : IStartupRegistration
{
    public bool IsEnabledResult { get; init; }
    public int IsEnabledCalls { get; private set; }
    public List<bool> SetEnabledCalls { get; } = [];

    public bool IsEnabled()
    {
        IsEnabledCalls++;
        return IsEnabledResult;
    }

    public void SetEnabled(bool enabled)
    {
        SetEnabledCalls.Add(enabled);
    }
}
