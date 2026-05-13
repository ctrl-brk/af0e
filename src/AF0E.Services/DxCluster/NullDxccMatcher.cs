namespace AF0E.Services.DxCluster;

internal sealed class NullDxccMatcher : IDxccMatcher
{
    public ValueTask<DxccMatch?> MatchAsync(Models.DxClusterSpot spot, CancellationToken cancellationToken = default)
        => ValueTask.FromResult<DxccMatch?>(null);
}
