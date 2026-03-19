namespace RigCommander.Contracts;

public sealed record WinkeyerSendRequest(string Text, int? Wpm, int? Repeat, int? RepeatDelaySeconds);
public sealed record WinkeyerSetWpmRequest(int Wpm);
