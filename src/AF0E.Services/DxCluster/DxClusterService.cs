using System.Net.Sockets;
using System.Text;
using AF0E.Services.DxCluster.Configuration;
using AF0E.Services.DxCluster.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AF0E.Services.DxCluster;

public sealed partial class DxClusterService : IDxClusterService, IAsyncDisposable
{
    private readonly IDxClusterEventsPublisher _eventsPublisher;
    private readonly ILogger<DxClusterService> _logger;
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);
    private readonly CancellationTokenSource _lifetimeCts = new();
    private readonly Lock _sync = new();
    private readonly Queue<DxClusterSpot> _spots = [];
    private readonly Dictionary<string, ServerRuntime> _servers;
    private readonly TimeSpan _inactivityTimeout;
    private readonly TimeSpan _reconnectDelay;
    private readonly TimeSpan _monitorInterval;
    private readonly TimeSpan _spotMaxAge;
    private readonly TimeSpan _loginDelay;
    private readonly int _maxSpots;
    private readonly Task _monitorTask;

    private SessionState? _session;
    private DateTimeOffset? _lastAccessUtc;
    private DateTimeOffset? _lastStartUtc;
    private DateTimeOffset? _lastStopUtc;

    public DxClusterService(IOptions<DxClusterOptions> options, IDxClusterEventsPublisher eventsPublisher, ILogger<DxClusterService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        _eventsPublisher = eventsPublisher;
        _logger = logger;

        var configuredOptions = options.Value;
        _inactivityTimeout = configuredOptions.InactivityTimeout > TimeSpan.Zero ? configuredOptions.InactivityTimeout : TimeSpan.FromMinutes(10);
        _reconnectDelay = configuredOptions.ReconnectDelay > TimeSpan.Zero ? configuredOptions.ReconnectDelay : TimeSpan.FromSeconds(15);
        _monitorInterval = configuredOptions.MonitorInterval > TimeSpan.Zero ? configuredOptions.MonitorInterval : TimeSpan.FromSeconds(15);
        _spotMaxAge = configuredOptions.SpotMaxAge > TimeSpan.Zero ? configuredOptions.SpotMaxAge : TimeSpan.FromMinutes(30);
        _loginDelay = configuredOptions.LoginDelay >= TimeSpan.Zero ? configuredOptions.LoginDelay : TimeSpan.FromSeconds(1);
        _maxSpots = configuredOptions.MaxSpots > 0 ? configuredOptions.MaxSpots : 250;

        _servers = configuredOptions.Servers
            .Select(CreateRuntime)
            .ToDictionary(static runtime => runtime.Key, StringComparer.OrdinalIgnoreCase);

        _monitorTask = MonitorInactivityAsync(_lifetimeCts.Token);
    }

    public async Task<IReadOnlyList<DxClusterSpot>> GetSpotsAsync(DateTimeOffset? sinceUtc, CancellationToken cancellationToken)
    {
        await EnsureRunningAsync(cancellationToken);

        lock (_sync)
        {
            var now = DateTimeOffset.UtcNow;
            PruneSpots(now);

            return _spots
                .Where(spot => sinceUtc is null || spot.SpotTimeUtc >= sinceUtc.Value)
                .OrderByDescending(static spot => spot.SpotTimeUtc)
                .ThenByDescending(static spot => spot.ReceivedAtUtc)
                .ToArray();
        }
    }

    public async Task<DxClusterStatus> GetStatusAsync(CancellationToken cancellationToken)
    {
        await EnsureRunningAsync(cancellationToken);

        lock (_sync)
        {
            PruneSpots(DateTimeOffset.UtcNow);
            return CreateStatusSnapshotUnsafe();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _lifetimeCts.CancelAsync();

        await StopAsync("service disposed", CancellationToken.None);

        try
        {
            await _monitorTask;
        }
        catch (OperationCanceledException)
        {
        }

        _lifecycleGate.Dispose();
        _lifetimeCts.Dispose();
    }

    private static ServerRuntime CreateRuntime(DxClusterServerOptions options, int index)
    {
        ArgumentNullException.ThrowIfNull(options);

        var name = string.IsNullOrWhiteSpace(options.Name) ? $"server-{index + 1}" : options.Name.Trim();
        return new ServerRuntime(name, new DxClusterServerOptions
        {
            Name = name,
            Host = options.Host.Trim(),
            Port = options.Port,
            Login = options.Login,
            Password = options.Password,
            PostLoginCommand = options.PostLoginCommand,
            Enabled = options.Enabled
        });
    }

    private async Task EnsureRunningAsync(CancellationToken cancellationToken)
    {
        UpdateLastAccessUtc();

        if (_servers.Values.All(static runtime => !runtime.Options.Enabled))
            return;

        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            UpdateLastAccessUtc();

            if (IsSessionActiveUnsafe())
                return;

            CancellationTokenSource? sessionCts = null;
            try
            {
                sessionCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetimeCts.Token);
                var tasks = _servers.Values
                    .Where(static runtime => runtime.Options.Enabled)
                    .Select(runtime => RunServerLoopAsync(runtime, sessionCts.Token))
                    .ToArray();

                _session = new SessionState(sessionCts, tasks);
                _lastStartUtc = DateTimeOffset.UtcNow;
                LogSessionStarting(tasks.Length);
                await PublishStatusSafeAsync(cancellationToken);
                sessionCts = null;
            }
            finally
            {
                sessionCts?.Dispose();
            }
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    private async Task StopAsync(string reason, CancellationToken cancellationToken)
    {
        await _lifecycleGate.WaitAsync(cancellationToken);
        try
        {
            var session = _session;
            if (session is null)
                return;

            _session = null;
            _lastStopUtc = DateTimeOffset.UtcNow;
            LogSessionStopping(reason);

            await session.CancellationSource.CancelAsync();

            try
            {
                await Task.WhenAll(session.Tasks);
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                session.CancellationSource.Dispose();
            }

            lock (_sync)
            {
                foreach (var runtime in _servers.Values.Where(runtime => runtime.Connected))
                {
                    runtime.Connected = false;
                    runtime.LastDisconnectUtc = DateTimeOffset.UtcNow;
                }
            }

            await PublishStatusSafeAsync(cancellationToken);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    private async Task MonitorInactivityAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_monitorInterval);

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            DateTimeOffset? lastAccessUtc;
            bool running;

            lock (_sync)
            {
                PruneSpots(DateTimeOffset.UtcNow);
                lastAccessUtc = _lastAccessUtc;
                running = IsSessionActiveUnsafe();
            }

            if (!running || lastAccessUtc is null)
                continue;

            if (DateTimeOffset.UtcNow - lastAccessUtc.Value < _inactivityTimeout)
                continue;

            await StopAsync($"inactivity timeout after {_inactivityTimeout}", cancellationToken);
        }
    }

    private async Task RunServerLoopAsync(ServerRuntime runtime, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ConnectAndReadAsync(runtime, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (IOException ex)
            {
                await SetDisconnectedAsync(runtime, ex.Message, cancellationToken);
                LogServerDisconnected(runtime.Options.Name, ex);
            }
            catch (SocketException ex)
            {
                await SetDisconnectedAsync(runtime, ex.Message, cancellationToken);
                LogServerSocketError(runtime.Options.Name, ex);
            }
            catch (InvalidOperationException ex)
            {
                await SetDisconnectedAsync(runtime, ex.Message, cancellationToken);
                LogServerProtocolError(runtime.Options.Name, ex);
            }

            if (!cancellationToken.IsCancellationRequested)
                await DelayReconnectAsync(runtime, cancellationToken);
        }
    }

    private async Task ConnectAndReadAsync(ServerRuntime runtime, CancellationToken cancellationToken)
    {
        runtime.ReconnectCount++;

        using var client = new TcpClient();
        await client.ConnectAsync(runtime.Options.Host, runtime.Options.Port, cancellationToken);

        await using var stream = client.GetStream();
        using var reader = new StreamReader(stream, Encoding.ASCII, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        await using var writer = CreateWriter(stream);

        await SetConnectedAsync(runtime, cancellationToken);
        await SendLoginIfConfiguredAsync(runtime, writer, cancellationToken);
        await SendPostLoginCommandIfConfiguredAsync(runtime, writer, cancellationToken);

        var passwordSent = false;
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken) ?? throw new IOException("The remote host closed the connection.");
            UpdateLastLine(runtime);

            if (!passwordSent && LooksLikePasswordPrompt(line) && !string.IsNullOrWhiteSpace(runtime.Options.Password))
            {
                await writer.WriteLineAsync(runtime.Options.Password.AsMemory(), cancellationToken);
                passwordSent = true;
                continue;
            }

            if (DxClusterSpotParser.TryParse(runtime.Options.Name, line, DateTimeOffset.UtcNow, out var spot) && spot is not null)
                await AddSpotAsync(runtime, spot, cancellationToken);
        }
    }

    private async Task SendLoginIfConfiguredAsync(ServerRuntime runtime, StreamWriter writer, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(runtime.Options.Login))
            return;

        if (_loginDelay > TimeSpan.Zero)
            await Task.Delay(_loginDelay, cancellationToken);

        await writer.WriteLineAsync(runtime.Options.Login.AsMemory(), cancellationToken);
    }

    private async Task SendPostLoginCommandIfConfiguredAsync(ServerRuntime runtime, StreamWriter writer, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(runtime.Options.PostLoginCommand))
            return;

        if (_loginDelay > TimeSpan.Zero)
            await Task.Delay(_loginDelay, cancellationToken);

        await writer.WriteLineAsync(runtime.Options.PostLoginCommand.AsMemory(), cancellationToken);
        LogPostLoginCommandSent(runtime.Options.Name, runtime.Options.PostLoginCommand);
    }

    private async Task DelayReconnectAsync(ServerRuntime runtime, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_reconnectDelay, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }

        LogServerReconnect(runtime.Options.Name);
    }

    private async Task AddSpotAsync(ServerRuntime runtime, DxClusterSpot spot, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            var now = DateTimeOffset.UtcNow;
            PruneSpots(now);
            _spots.Enqueue(spot);
            runtime.LastSpotUtc = now;
            runtime.LastError = null;
            runtime.LastErrorUtc = null;

            while (_spots.Count > _maxSpots)
                _spots.Dequeue();
        }

        await PublishSpotSafeAsync(spot, cancellationToken);
    }

    private async Task SetConnectedAsync(ServerRuntime runtime, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            runtime.Connected = true;
            runtime.LastConnectUtc = DateTimeOffset.UtcNow;
            runtime.LastError = null;
            runtime.LastErrorUtc = null;
        }

        LogServerConnected(runtime.Options.Name, runtime.Options.Host, runtime.Options.Port);
        await PublishStatusSafeAsync(cancellationToken);
    }

    private async Task SetDisconnectedAsync(ServerRuntime runtime, string? error, CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            runtime.Connected = false;
            runtime.LastDisconnectUtc = DateTimeOffset.UtcNow;
            runtime.LastError = error;
            runtime.LastErrorUtc = string.IsNullOrWhiteSpace(error) ? null : DateTimeOffset.UtcNow;
        }

        await PublishStatusSafeAsync(cancellationToken);
    }

    private void UpdateLastLine(ServerRuntime runtime)
    {
        lock (_sync)
        {
            runtime.LastLineUtc = DateTimeOffset.UtcNow;
        }
    }

    private void UpdateLastAccessUtc()
    {
        lock (_sync)
        {
            _lastAccessUtc = DateTimeOffset.UtcNow;
        }
    }

    private void PruneSpots(DateTimeOffset now)
    {
        while (_spots.TryPeek(out var spot) && now - spot.ReceivedAtUtc > _spotMaxAge)
            _spots.Dequeue();
    }

    private DxClusterStatus CreateStatusSnapshotUnsafe()
        => new()
        {
            Configured = _servers.Values.Any(static runtime => runtime.Options.Enabled),
            Running = IsSessionActiveUnsafe(),
            LastAccessUtc = _lastAccessUtc,
            LastStartUtc = _lastStartUtc,
            LastStopUtc = _lastStopUtc,
            CachedSpotCount = _spots.Count,
            InactivityTimeout = _inactivityTimeout,
            ReconnectDelay = _reconnectDelay,
            Servers = _servers.Values
                .OrderBy(static runtime => runtime.Options.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(static runtime => runtime.Options.Host, StringComparer.OrdinalIgnoreCase)
                .Select(static runtime => runtime.ToStatus())
                .ToArray()
        };

    private async ValueTask PublishSpotSafeAsync(DxClusterSpot spot, CancellationToken cancellationToken)
    {
        try
        {
            await _eventsPublisher.PublishSpotAsync(spot, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || _lifetimeCts.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            LogSpotPublishFailed(ex);
        }
    }

    private async ValueTask PublishStatusSafeAsync(CancellationToken cancellationToken)
    {
        DxClusterStatus status;
        lock (_sync)
        {
            PruneSpots(DateTimeOffset.UtcNow);
            status = CreateStatusSnapshotUnsafe();
        }

        try
        {
            await _eventsPublisher.PublishStatusAsync(status, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || _lifetimeCts.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            LogStatusPublishFailed(ex);
        }
    }

    private bool IsSessionActiveUnsafe()
    {
        var session = _session;
        return session is not null
               && !session.CancellationSource.IsCancellationRequested
               && session.Tasks.Any(static task => !task.IsCompleted);
    }

    private static StreamWriter CreateWriter(Stream stream)
        => new(stream, Encoding.ASCII, leaveOpen: true)
        {
            AutoFlush = true,
            NewLine = "\r\n"
        };

    private static bool LooksLikePasswordPrompt(string line)
        => line.Contains("password", StringComparison.OrdinalIgnoreCase);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Starting DX cluster session with {ServerCount} configured server(s)")]
    private partial void LogSessionStarting(int serverCount);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Stopping DX cluster session: {Reason}")]
    private partial void LogSessionStopping(string reason);

    [LoggerMessage(Level = LogLevel.Debug, Message = "DX cluster server {ServerName} disconnected")]
    private partial void LogServerDisconnected(string serverName, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "DX cluster server {ServerName} socket error")]
    private partial void LogServerSocketError(string serverName, Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "DX cluster server {ServerName} protocol error")]
    private partial void LogServerProtocolError(string serverName, Exception exception);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Reconnecting DX cluster server {ServerName}")]
    private partial void LogServerReconnect(string serverName);

    [LoggerMessage(Level = LogLevel.Information, Message = "Connected to DX cluster server {ServerName} ({Host}:{Port})")]
    private partial void LogServerConnected(string serverName, string host, int port);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Sent DX cluster post-login command to {ServerName}: {Command}")]
    private partial void LogPostLoginCommandSent(string serverName, string command);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Publishing DX cluster spot realtime event failed")]
    private partial void LogSpotPublishFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Publishing DX cluster status realtime event failed")]
    private partial void LogStatusPublishFailed(Exception exception);

    private sealed class SessionState(CancellationTokenSource cancellationSource, IReadOnlyList<Task> tasks)
    {
        public CancellationTokenSource CancellationSource { get; } = cancellationSource;
        public IReadOnlyList<Task> Tasks { get; } = tasks;
    }

    private sealed class ServerRuntime(string key, DxClusterServerOptions options)
    {
        public string Key { get; } = key;
        public DxClusterServerOptions Options { get; } = options;
        public bool Connected { get; set; }
        public int ReconnectCount { get; set; }
        public DateTimeOffset? LastConnectUtc { get; set; }
        public DateTimeOffset? LastDisconnectUtc { get; set; }
        public DateTimeOffset? LastLineUtc { get; set; }
        public DateTimeOffset? LastSpotUtc { get; set; }
        public DateTimeOffset? LastErrorUtc { get; set; }
        public string? LastError { get; set; }

        public DxClusterServerStatus ToStatus()
            => new()
            {
                Name = Options.Name,
                Host = Options.Host,
                Port = Options.Port,
                Enabled = Options.Enabled,
                Connected = Connected,
                ReconnectCount = ReconnectCount,
                LastConnectUtc = LastConnectUtc,
                LastDisconnectUtc = LastDisconnectUtc,
                LastLineUtc = LastLineUtc,
                LastSpotUtc = LastSpotUtc,
                LastErrorUtc = LastErrorUtc,
                LastError = LastError
            };
    }
}
