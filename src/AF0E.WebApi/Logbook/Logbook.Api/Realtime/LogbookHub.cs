using AF0E.Services.DxCluster;
using AF0E.Services.DxCluster.Models;
using Microsoft.AspNetCore.SignalR;

namespace Logbook.Api.Realtime;

public sealed class LogbookHub(DxClusterHubSessionManager dxClusterSessionManager, IDxClusterService dxClusterService) : Hub
{
	public async Task<DxClusterStatus> SubscribeDxCluster()
	{
		var cancellationToken = Context.ConnectionAborted;
		await Groups.AddToGroupAsync(Context.ConnectionId, DxClusterHubGroups.GroupName, cancellationToken);
		await dxClusterSessionManager.SubscribeAsync(Context.ConnectionId, cancellationToken);

		return await dxClusterService.GetStatusAsync(cancellationToken);
	}

	public async Task UnsubscribeDxCluster()
	{
		var cancellationToken = Context.ConnectionAborted;
		await Groups.RemoveFromGroupAsync(Context.ConnectionId, DxClusterHubGroups.GroupName, cancellationToken);
		await dxClusterSessionManager.UnsubscribeAsync(Context.ConnectionId);
	}

	public override async Task OnDisconnectedAsync(Exception? exception)
	{
		await dxClusterSessionManager.UnsubscribeAsync(Context.ConnectionId);
		await base.OnDisconnectedAsync(exception);
	}
}

