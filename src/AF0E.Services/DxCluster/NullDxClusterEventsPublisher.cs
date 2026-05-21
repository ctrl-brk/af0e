using AF0E.Services.DxCluster.Models;

namespace AF0E.Services.DxCluster;

internal sealed class NullDxClusterEventsPublisher : IDxClusterEventsPublisher
{
    public ValueTask PublishSpotAsync(DxClusterSpot spot, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

    public ValueTask PublishStatusAsync(DxClusterStatus status, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
}
