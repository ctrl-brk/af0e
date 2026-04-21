namespace RigCommander.Radios.Icom;

public enum IcomMode : byte
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

public sealed record IcomRadioStatus(long FrequencyHz, IcomMode Mode, byte Filter, bool DataModeOn, bool NoiseReductionOn, bool NoiseBlankerOn, bool SplitOn)
{
    public string DisplayMode => DataModeOn && Mode is IcomMode.USB or IcomMode.LSB ? $"{Mode}-D" : Mode.ToString();
}
