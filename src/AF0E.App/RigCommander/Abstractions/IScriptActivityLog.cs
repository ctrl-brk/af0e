namespace RigCommander.Abstractions;

public interface IScriptActivityLog
{
    void Log(ActivityLogLevel level, string message);

    void LogDebug(string message)       => Log(ActivityLogLevel.Debug,       message);
    void LogInformation(string message) => Log(ActivityLogLevel.Information, message);
    void LogWarning(string message)     => Log(ActivityLogLevel.Warning,     message);
    void LogError(string message)       => Log(ActivityLogLevel.Error,       message);
}
