// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
namespace PotaParksImport;

public class AppSettings
{
    public string ConnectionString { get; init; } = null!;
    public Uri ParksUrl { get; init; } = null!;
    public int HttpTimeoutSeconds { get; init; } = 60;
    public bool DryRun { get; init; }
}
