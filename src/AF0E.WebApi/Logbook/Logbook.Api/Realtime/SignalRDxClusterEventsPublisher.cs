using AF0E.Services.DxCluster;
using AF0E.Services.DxCluster.Models;
using Microsoft.AspNetCore.SignalR;

namespace Logbook.Api.Realtime;

public sealed class SignalRDxClusterEventsPublisher(IHubContext<LogbookHub> hubContext) : IDxClusterEventsPublisher
{
    public ValueTask PublishSpotAsync(DxClusterSpot spot, CancellationToken cancellationToken = default)
        => new(hubContext.Clients.Group(DxClusterHubGroups.GroupName).SendAsync("dxcluster.spot", spot, cancellationToken));

    public ValueTask PublishStatusAsync(DxClusterStatus status, CancellationToken cancellationToken = default)
        => new(hubContext.Clients.Group(DxClusterHubGroups.GroupName).SendAsync("dxcluster.status", status, cancellationToken));
}

