namespace RigCommander.Abstractions;

public interface IScriptActivityLog
{
    void AppendLine(string message);
}
