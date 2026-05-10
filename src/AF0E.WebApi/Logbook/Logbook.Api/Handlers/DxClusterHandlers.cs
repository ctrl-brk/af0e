using AF0E.Services.DxCluster;
using AF0E.Services.DxCluster.Models;

namespace Logbook.Api.Handlers;

public static class DxClusterHandlers
{
    public static Task<IReadOnlyList<DxClusterSpot>> GetSpots(DateTimeOffset? sinceUtc, IDxClusterService dxClusterService, CancellationToken cancellationToken)
        => dxClusterService.GetSpotsAsync(sinceUtc, cancellationToken);

    public static Task<DxClusterStatus> GetStatus(IDxClusterService dxClusterService, CancellationToken cancellationToken)
        => dxClusterService.GetStatusAsync(cancellationToken);
}
