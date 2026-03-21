// ReSharper disable ClassNeverInstantiated.Global
namespace RigCommander.Contracts;

public sealed record SetFrequencyRequest(long FrequencyHz);
public sealed record SetModeRequest(string Mode);
public sealed record SetStatusRequest(long? FrequencyHz, string? Mode, byte? Filter);

public sealed record RadioStatus(long FrequencyHz, string Mode, byte? Filter, bool DataModeOn, bool NoiseReductionOn, bool NoiseBlankerOn, bool SplitOn);
