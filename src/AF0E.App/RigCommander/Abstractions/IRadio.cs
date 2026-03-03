using RigCommander.Contracts;

namespace RigCommander.Abstractions;

public interface IRadio : IDisposable
{
    T WithConnection<T>(Func<T> action);

    void SetFrequency(long frequencyHz);
    void SetMode(string modeText);

    long GetFrequency();
    RadioStatus GetStatus();
}
