using System.Diagnostics;
using System.Text.Json;
using System.Threading.Channels;
using RigCommander.Abstractions;

namespace RigCommander.Services;

#pragma warning disable CA1848 // Use the LoggerMessage delegates
#pragma warning disable CA1873

public sealed record AdifForwardingItem(
    // ReSharper disable NotAccessedPositionalProperty.Global
    DateTime ReceivedUtc,
    string Source,
    string RemoteEndPoint,
    string RawAdif,
    // ReSharper restore NotAccessedPositionalProperty.Global
    IReadOnlyDictionary<string, string> Fields);

public sealed class AdifApiForwarder(
    AdifForwardingSettings settings,
    ILogger<AdifApiForwarder> logger,
    IScriptActivityLog? activityLog = null,
    ActivationIdStore? activationIdStore = null)
    : IDisposable
{
    private readonly Channel<AdifForwardingItem> _channel = Channel.CreateBounded<AdifForwardingItem>(new BoundedChannelOptions(settings.QueueCapacity)
    {
        SingleReader = true,
        SingleWriter = false,
        FullMode = BoundedChannelFullMode.DropWrite
    });

    private readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds)
    };

    private readonly Uri _endpointUri = new(settings.EndpointUrl!, UriKind.Absolute);

    private CancellationTokenSource? _workerCts;
    private bool _started;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_started)
            return Task.CompletedTask;

        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation(settings.ApiKeyHeaderName, settings.ApiKey);

        _started = true;
        _workerCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        Task.Run(() => WorkerLoopAsync(_workerCts.Token), _workerCts.Token);
        logger.LogInformation("ADIF API forwarding started; endpoint={Endpoint}", _endpointUri);
        return Task.CompletedTask;
    }

    public bool TryEnqueue(AdifForwardingItem item)
    {
        if (_channel.Writer.TryWrite(item))
            return true;

        logger.LogWarning("ADIF forwarding queue full; dropping record call={Call}", item.Fields.TryGetValue("CALL", out var call) ? call : "-");
        activityLog?.AppendLine("[ADIF UDP] Forwarding queue full. Dropped record.");
        return false;
    }

    private async Task WorkerLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _channel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (_channel.Reader.TryRead(out var item))
                {
                    await SendWithRetryAsync(item, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown path.
        }
    }

    private async Task SendWithRetryAsync(AdifForwardingItem item, CancellationToken cancellationToken)
    {
        if (TryGetBlockingProcess(out var processName))
        {
            activityLog?.AppendLine($"[ADIF UDP] Skipped API forward: '{processName}' is running.");
            return;
        }

        var activationId = activationIdStore?.Get();

        if (!AdifQsoRequestMapper.TryMap(item.Fields, activationId, out var payload, out var mappingError))
        {
            logger.LogWarning("Skipping ADIF forward because mapping failed: {Error}", mappingError);
            activityLog?.AppendLine($"[ADIF UDP] Skipped API forward: {mappingError}");
            return;
        }

        for (var attempt = 0; attempt <= settings.MaxRetries; attempt++)
        {
            try
            {
                using var response = await _httpClient.PostAsJsonAsync(_endpointUri, payload, JsonSerializerOptions.Web, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    activityLog?.AppendLine($"[ADIF UDP] Forwarded QSO to API: call={payload.Qso.Call}, band={payload.Qso.Band}, mode={payload.Qso.Mode}");
                    return;
                }

                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                logger.LogWarning(
                    "ADIF forwarding failed with HTTP {StatusCode} on attempt {Attempt}/{Attempts}; body={Body}",
                    (int)response.StatusCode,
                    attempt + 1,
                    settings.MaxRetries + 1,
                    body.Length > 200 ? body[..200] + "..." : body);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "ADIF forwarding failed on attempt {Attempt}/{Attempts}",
                    attempt + 1,
                    settings.MaxRetries + 1);
            }

            if (attempt >= settings.MaxRetries)
                break;

            if (settings.RetryDelayMs > 0)
                await Task.Delay(settings.RetryDelayMs, cancellationToken);
        }

        activityLog?.AppendLine("[ADIF UDP] Failed to forward ADIF record after retries.");
    }

    private bool TryGetBlockingProcess(out string processName)
    {
        foreach (var configuredName in settings.SkipWhenProcessRunning)
        {
            var candidate = configuredName.Trim();
            if (candidate.Length == 0)
                continue;

            var probe = candidate.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)
                ? candidate[..^4]
                : candidate;

            var processes = Process.GetProcessesByName(probe);
            var found = processes.Length > 0;

            foreach (var process in processes)
                process.Dispose();

            if (!found)
                continue;

            processName = candidate;
            return true;
        }

        processName = string.Empty;
        return false;
    }

    public void Dispose()
    {
        try
        {
            _workerCts?.Cancel();
        }
        catch
        {
            // Ignore cancellation race during shutdown.
        }

        _workerCts?.Dispose();
        _httpClient.Dispose();
    }
}
