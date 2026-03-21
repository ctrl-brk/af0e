using RigCommander.Abstractions;
using RigCommander.Contracts;

namespace RigCommander.Radios.Icom;

// ReSharper disable once InconsistentNaming
public sealed class IC_9100(string portName, int baudRate, byte radioAddress, byte controllerAddress) : IRadio
{
    private readonly CivIcomSerial _civ = new(portName, baudRate, radioAddress, controllerAddress);

    public T WithConnection<T>(Func<T> action) => _civ.WithConnection(action);

    public long GetFrequency() => _civ.GetFrequency();

    public void SetFrequency(long frequencyHz) => _civ.SetFrequency(frequencyHz);

    public void SetMode(string modeText, byte filter = 0x01)
    {
        (IcomMode baseMode, var dataOn) = ParseIcomMode(modeText);
        _civ.SetMode(baseMode, filter);
        _civ.SetDataMode(dataOn);
    }

    public void SetNoiseReduction(bool enabled)
    {
        _civ.SetNoiseReduction(enabled);
    }

    public void SetNoiseBlanker(bool enabled)
    {
        _civ.SetNoiseBlanker(enabled);
    }

    public RadioStatus GetStatus()
    {
        var st = _civ.GetStatus();
        return new RadioStatus(st.FrequencyHz, st.DisplayMode, st.Filter, st.DataModeOn, NoiseReductionOn: st.NoiseReductionOn, NoiseBlankerOn: st.NoiseBlankerOn, st.SplitOn);
    }

    private static (IcomMode baseMode, bool dataOn) ParseIcomMode(string modeText)
    {
        var raw = modeText.Trim();

        var isUsbD =
            raw.Equals("USB-D", StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("USBD", StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("FT8", StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("FT4", StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("FT2", StringComparison.OrdinalIgnoreCase);

        var isLsbD =
            raw.Equals("LSB-D", StringComparison.OrdinalIgnoreCase) ||
            raw.Equals("LSBD", StringComparison.OrdinalIgnoreCase);

        if (isUsbD) return (IcomMode.USB, true);
        if (isLsbD) return (IcomMode.LSB, true);

        return !Enum.TryParse<IcomMode>(raw, ignoreCase: true, out var parsed) ?
            throw new ArgumentException("Unsupported Mode for IC-9100. Try: LSB, USB, CW, AM, FM, WFM, RTTY, RTTYR, USB-D, LSB-D, FT8, FT4, FT2") :
            (parsed, false);
    }

    public void Dispose() => _civ.Dispose();
}

public sealed record IcomRadioStatus(long FrequencyHz, IcomMode Mode, byte Filter, bool DataModeOn, bool NoiseReductionOn, bool NoiseBlankerOn, bool SplitOn)
{
    public string DisplayMode => DataModeOn && Mode is IcomMode.USB or IcomMode.LSB ? $"{Mode}-D" : Mode.ToString();
}

#pragma warning disable CA1028
public enum IcomMode : byte
#pragma warning restore CA1028
{
    LSB = 0x00,
    USB = 0x01,
    AM = 0x02,
    CW = 0x03,
    RTTY = 0x04,
    FM = 0x05,
    WFM = 0x06,
    CW_R = 0x07,
    RTTYR = 0x08
}
