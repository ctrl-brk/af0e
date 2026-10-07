namespace AF0E.DB.Models;

public sealed class HamEventContact
{
    public int ContactId { get; init; }
    public int HamEventId { get; init; }
    public int LogId { get; init; }

    public HamEvent HamEvent { get; init; } = null!;
    public HrdLog Log { get; init; } = null!;
}
