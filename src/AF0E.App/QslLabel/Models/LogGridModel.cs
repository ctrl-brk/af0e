namespace QslLabel.Models;

internal class LogGridModel
{
    public string Call { get; init; } = null!;
    public DateTime UTC { get; init; }
    public string? sQSL { get; init; }
    public string? Via { get; init; }
    public string? QslComment { get; init; }
    public string? rQSL { get; init; }
    public string Mode { get; init; } = null!;
    public string? RST { get; init; }
    public string Band { get; init; } = null!;
    public string Mhz { get; set; } = null!;
    public string ParkNum { get; init; } = null!;
    public string POTA { get; init; } = null!;
    public string? P2P { get; init; }
    public string? Sat{ get; init; }
    public string? Name { get; init; }
    public string? Country { get; init; }
    public string? Comment { get; init; }
    public string? MyGrid { get; init; }
    public string? MyState { get; init; }
    public string? MyCity { get; init; }
    public string? MyCounty { get; init; }
    public int ID { get; init; }
}
