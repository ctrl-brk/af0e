namespace RigCommander.Contracts;

public sealed record WinkeyerSendRequest(string Text, int? Repeat, int? RepeatDelaySeconds);
public sealed record WinkeyerSetWpmRequest(int Wpm);
