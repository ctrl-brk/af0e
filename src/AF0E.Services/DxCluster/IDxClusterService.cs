using AF0E.Services.DxCluster.Models;

namespace AF0E.Services.DxCluster;

public interface IDxClusterService
{
    Task<IReadOnlyList<DxClusterSpot>> GetSpotsAsync(DateTimeOffset? sinceUtc, string? filterName, CancellationToken cancellationToken);
    Task<DxClusterStatus> GetStatusAsync(CancellationToken cancellationToken);
}
