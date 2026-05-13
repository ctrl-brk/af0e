// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace AF0E.Services.DxCluster.Models;

public sealed record DxClusterSpot
{
    public required string SourceName { get; init; }
    public required string SpotterCallsign { get; init; }
    public required string DxCallsign { get; init; }
    public int? DxccEntityCode { get; init; }
    public string? DxccEntityName { get; init; }
    public string? DxccCountryCode { get; init; }
    public string? DxccWorkedStatus { get; init; }
    public decimal FrequencyKhz { get; init; }
    public string? Mode { get; init; }
    public string Comment { get; init; } = string.Empty;
    public required string RawLine { get; init; }
    public DateTimeOffset SpotTimeUtc { get; init; }
    public DateTimeOffset ReceivedAtUtc { get; init; }
}
