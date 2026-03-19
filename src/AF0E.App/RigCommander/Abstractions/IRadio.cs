using RigCommander.Contracts;

namespace RigCommander.Abstractions;

public interface IRadio : IDisposable
{
    T WithConnection<T>(Func<T> action);
    long GetFrequency();
    void SetFrequency(long frequencyHz);
    void SetMode(string modeText, byte filter);
    void SetNoiseReduction(bool enabled);
    void SetNoiseBlanker(bool enabled);

    RadioStatus GetStatus();
}
