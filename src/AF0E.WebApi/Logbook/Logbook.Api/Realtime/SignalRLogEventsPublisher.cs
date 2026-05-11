using Microsoft.AspNetCore.SignalR;

namespace Logbook.Api.Realtime;

public sealed class SignalRLogEventsPublisher(IHubContext<LogbookHub> hubContext) : ILogEventsPublisher
{
    public Task PublishAsync(LogChangedEvent evt, CancellationToken ct = default)
        => hubContext.Clients.All.SendAsync("log.changed", evt, ct);
}
