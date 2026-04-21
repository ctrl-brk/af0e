namespace RigCommander.Abstractions;

public interface IRunKeyProvider
{
    IRunKeySession? OpenRunKey(bool writable);
}

public interface IRunKeySession : IDisposable
{
    object? GetValue(string name);
    void SetValue(string name, string value);
    void DeleteValue(string name, bool throwOnMissingValue);
}
