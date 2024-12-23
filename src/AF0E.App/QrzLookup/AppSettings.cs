namespace QrzLookup;

public class AppSettings
{
    public string ConnectionString { get; set; } = null!;
    public Uri QrzApiUrl { get; init; } = null!;
    public string QrzUser { get; init; } = null!;
    public string QrzPassword { get; init; } = null!;
    public int BatchSize { get; init; }
}
