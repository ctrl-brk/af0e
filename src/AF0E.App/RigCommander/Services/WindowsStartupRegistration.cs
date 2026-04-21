using Microsoft.Win32;
using RigCommander.Abstractions;

namespace RigCommander.Services;

public sealed class WindowsStartupRegistration(
    string valueName,
    IRunKeyProvider runKeyProvider,
    IExecutablePathProvider executablePathProvider) : IStartupRegistration
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public WindowsStartupRegistration(string valueName)
        : this(valueName, new WindowsRunKeyProvider(), new ApplicationExecutablePathProvider())
    {
    }

    public bool IsEnabled()
    {
        using var key = runKeyProvider.OpenRunKey(writable: false);
        return key?.GetValue(valueName) is string;
    }

    public void SetEnabled(bool enabled)
    {
        using var key = runKeyProvider.OpenRunKey(writable: true);
        if (key is null)
            return;

        if (enabled)
        {
            var exePath = executablePathProvider.GetExecutablePath();
            key.SetValue(valueName, $"\"{exePath}\"");
            return;
        }

        key.DeleteValue(valueName, false);
    }

    private sealed class WindowsRunKeyProvider : IRunKeyProvider
    {
        public IRunKeySession? OpenRunKey(bool writable)
        {
            var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable);
            return key is null ? null : new RegistryRunKeySession(key);
        }
    }

    private sealed class ApplicationExecutablePathProvider : IExecutablePathProvider
    {
        public string GetExecutablePath() => Application.ExecutablePath;
    }

    private sealed class RegistryRunKeySession(RegistryKey key) : IRunKeySession
    {
        public object? GetValue(string name) => key.GetValue(name);

        public void SetValue(string name, string value) => key.SetValue(name, value);

        public void DeleteValue(string name, bool throwOnMissingValue) => key.DeleteValue(name, throwOnMissingValue);

        public void Dispose() => key.Dispose();
    }
}
