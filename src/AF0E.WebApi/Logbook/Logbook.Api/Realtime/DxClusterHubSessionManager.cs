using System.Collections.Concurrent;
using AF0E.Services.DxCluster;

namespace Logbook.Api.Realtime;

public sealed partial class DxClusterHubSessionManager(IDxClusterService dxClusterService, ILogger<DxClusterHubSessionManager> logger) : IAsyncDisposable
{
    private static readonly TimeSpan _keepAliveInterval = TimeSpan.FromMinutes(1);

    private readonly ConcurrentDictionary<string, byte> _subscriptions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Lock _sync = new();
    private CancellationTokenSource? _keepAliveCts;
    private Task? _keepAliveTask;

    public async Task SubscribeAsync(string connectionId, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);

        var added = _subscriptions.TryAdd(connectionId, 0);
        if (added)
            EnsureKeepAliveLoop();

        await dxClusterService.GetStatusAsync(cancellationToken);
    }

    public async Task UnsubscribeAsync(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
            return;

        _subscriptions.TryRemove(connectionId, out _);
        await StopKeepAliveLoopIfIdleAsync();
    }

    public async ValueTask DisposeAsync()
    {
        CancellationTokenSource? cts;
        Task? task;

        lock (_sync)
        {
            cts = _keepAliveCts;
            task = _keepAliveTask;
            _keepAliveCts = null;
            _keepAliveTask = null;
        }

        if (cts is null)
            return;

        await cts.CancelAsync();
        try
        {
            if (task is not null)
                await task;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            cts.Dispose();
        }
    }

    private void EnsureKeepAliveLoop()
    {
        lock (_sync)
        {
            if (_keepAliveTask is { IsCompleted: false })
                return;

            _keepAliveCts = new CancellationTokenSource();
            _keepAliveTask = RunKeepAliveLoopAsync(_keepAliveCts.Token);
        }
    }

    private async Task StopKeepAliveLoopIfIdleAsync()
    {
        CancellationTokenSource? cts;
        Task? task;

        lock (_sync)
        {
            if (!_subscriptions.IsEmpty)
                return;

            cts = _keepAliveCts;
            task = _keepAliveTask;
            _keepAliveCts = null;
            _keepAliveTask = null;
        }

        if (cts is null)
            return;

        await cts.CancelAsync();
        try
        {
            if (task is not null)
                await task;
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            cts.Dispose();
        }
    }

    private async Task RunKeepAliveLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_keepAliveInterval);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                if (_subscriptions.IsEmpty)
                    continue;

                try
                {
                    await dxClusterService.GetStatusAsync(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    LogDxClusterKeepAliveFailed(ex);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "DX cluster SignalR keep-alive tick failed")]
    private partial void LogDxClusterKeepAliveFailed(Exception exception);
}


