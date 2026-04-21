using RigCommander.Abstractions;

namespace RigCommander.Services;

public sealed class HostShutdownService(IHost host) : IHostShutdownService
{
    public async Task StopAsync(TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);
        await host.StopAsync(timeoutCts.Token);
    }
}
