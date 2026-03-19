namespace AF0E.Services.Pota.Models;

public record PotaSpot
{
    public long SpotId { get; init; }
    public string Activator { get; init; } = string.Empty;
    public string? ActivatorLastSpotTime { get; init; }
    public string? ActivatorLastComments { get; init; }
    public Frequency Frequency { get; init; } = new(null);
    public string Mode { get; init; } = string.Empty;
    public string Reference { get; init; } = string.Empty;
    public string? ParkName { get; init; }
    public DateTime SpotTime { get; init; }
    public string Spotter { get; init; } = string.Empty;
    public string? Comments { get; init; }
    public string? Source { get; init; }
    public string? Invalid { get; init; }
    public string? Name { get; init; }
    public string? LocationDesc { get; init; }
    public string? Grid4 { get; init; }
    public string? Grid6 { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public int? Count { get; init; }
    public int? Expire { get; init; }

    public bool Qrt => ActivatorLastComments?.Contains("QRT", StringComparison.OrdinalIgnoreCase) == true || Comments?.Contains("QRT", StringComparison.OrdinalIgnoreCase) == true;
}
