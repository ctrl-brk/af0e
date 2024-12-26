namespace PotaLookup;

public class AppSettings
{
    public string ConnectionString { get; set; } = null!;
    public Uri PotaApiUrl { get; init; } = null!;
    public string LogbookRoute { get; init; } = null!;
    public string ParkInfoRoute { get; init; } = null!;
    public string AuthTokenFileName { get; init; } = null!;
}
