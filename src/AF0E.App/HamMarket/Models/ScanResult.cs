namespace HamMarket.Models;

public class ScanResult
{
    public int Items { get; init; }
    public DateTime LastScan { get; init; }
    public string Title { get; init; } = null!;
    public string Html { get; init; } = null!;
}
