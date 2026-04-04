namespace RigCommander.Contracts;

public sealed record WinkeyerSendRequest(string Text, bool? RigControl, int? Wpm, int? Repeat, int? RepeatDelaySeconds);
public sealed record WinkeyerSetWpmRequest(int Wpm);
