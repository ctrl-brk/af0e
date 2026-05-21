namespace AF0E.Services.DxCluster;

public interface IDxccMatcher
{
    ValueTask<DxccMatch?> MatchAsync(Models.DxClusterSpot spot, CancellationToken cancellationToken = default);
}
