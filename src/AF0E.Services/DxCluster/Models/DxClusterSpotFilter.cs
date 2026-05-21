// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace AF0E.Services.DxCluster.Models;

public sealed record DxClusterSpotFilter
{
    public required string Name { get; init; }
    public string? CallsignPatterns { get; init; }
    public IReadOnlyList<DxClusterFrequencyWindow>? FrequencyWindows { get; init; }
    public IReadOnlyList<string>? Modes { get; init; }
    public IReadOnlyList<string> InvalidCallsignPatterns { get; init; } = [];
}

public sealed record DxClusterFrequencyWindow
{
    public decimal? MinFrequencyKhz { get; init; }
    public decimal? MaxFrequencyKhz { get; init; }
}
