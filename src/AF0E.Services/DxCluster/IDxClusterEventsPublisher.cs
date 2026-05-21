using AF0E.Services.DxCluster.Models;

namespace AF0E.Services.DxCluster;

public interface IDxClusterEventsPublisher
{
    ValueTask PublishSpotAsync(DxClusterSpot spot, CancellationToken cancellationToken = default);
    ValueTask PublishStatusAsync(DxClusterStatus status, CancellationToken cancellationToken = default);
}
