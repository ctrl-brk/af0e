namespace AF0E.Services.DxCluster;

public sealed record DxccMatch
{
    public int EntityCode { get; init; }
    public required string EntityName { get; init; }
    public string? CountryCode { get; init; }
    public DxccWorkedStatus WorkedStatus { get; init; }
}
