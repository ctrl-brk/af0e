namespace QrzLookup;

public class AppSettings
{
    public string ConnectionString { get; set; } = null!;
    public int BatchSize { get; init; }
}
