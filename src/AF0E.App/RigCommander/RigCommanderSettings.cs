namespace RigCommander;

public sealed class RigCommanderSettings
{
    public string? ActiveProfile { get; set; }
    public string? ListenPort { get; init; }
    public int StatusDelayMs { get; init; } = 1000;
#pragma warning disable CA1002 // Do not expose generic lists
    public List<RadioProfileSettings> Profiles { get; init; } = [];
#pragma warning restore CA1002
    public WinkeyerSettings? Winkeyer { get; init; }
    public Ui Ui { get; init; } = new();

    public RadioProfileSettings? FindProfileByName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name) || Profiles.Count == 0)
            return null;

        return Profiles.FirstOrDefault(p =>
            string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class RadioProfileSettings
{
    public string Name { get; init; } = "default";
    public RadioProfileKind Kind { get; init; } = RadioProfileKind.Icom;
    public IcomSettings? Icom { get; init; }
    public YaesuSettings? Yaesu { get; init; }
}

public enum RadioProfileKind
{
    Icom,
    Yaesu
}

public sealed class IcomSettings
{
    public required string PortName { get; init; }
    public required int BaudRate { get; init; }
    public byte RadioAddress { get; init; } = 0x7C;
    public byte ControllerAddress { get; init; } = 0xE0;
}

public sealed class YaesuSettings
{
    public required string PortName { get; init; }
    public required int BaudRate { get; init; }
    public bool? DtrEnable { get; init; }
    public bool? RtsEnable { get; init; }
    public int ReplyDelayMs { get; init; } = 40;
    public int ReadTimeoutMs { get; init; } = 1000;
}

public sealed class WinkeyerSettings
{
    public bool Enabled { get; init; }
    public required string PortName { get; init; }
    public int BaudRate { get; init; } = 1200;
    public bool KeepHostOpen { get; init; } = true;
    public int IdleCloseSeconds { get; init; } = 60;
    public int MinWpm { get; set; } = 10;
    public int MaxWpm { get; set; } = 35;
}

public sealed class Ui
{
    public bool StartMinimized { get; init; } = false;
}
