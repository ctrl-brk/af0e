using RigCommander.Abstractions;

namespace RigCommander.Tests.TestDoubles;

internal sealed class FakeRunKeyProvider(FakeRunKeySession? runKey) : IRunKeyProvider
{
    public IRunKeySession? OpenRunKey(bool writable) => runKey;
}

internal sealed class FakeExecutablePathProvider(string executablePath) : IExecutablePathProvider
{
    public string GetExecutablePath() => executablePath;
}

internal sealed class FakeRunKeySession : IRunKeySession
{
    public Dictionary<string, object?> Values { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<(string Name, bool ThrowOnMissingValue)> DeleteCalls { get; } = [];

    public object? GetValue(string name)
    {
        return Values.TryGetValue(name, out var value) ? value : null;
    }

    public void SetValue(string name, string value)
    {
        Values[name] = value;
    }

    public void DeleteValue(string name, bool throwOnMissingValue)
    {
        DeleteCalls.Add((name, throwOnMissingValue));
        Values.Remove(name);
    }

    public void Dispose()
    {
        // no-op for tests
    }
}
