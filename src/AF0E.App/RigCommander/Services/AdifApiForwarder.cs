using System.Globalization;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

[SuppressMessage("Design", "CA1054:URI-like parameters should not be strings")]
public sealed class AdifApiForwarder(
    string logbookApiUrl,
    AdifForwardingSettings settings,
    ILogger<AdifApiForwarder> logger,
    IScriptActivityLog? activityLog = null,
    ActivationIdStore? activationIdStore = null)
    : IDisposable
{
    private const string ForwardingEndpointPath = "logbook/qso";
    private const string ActivationsEndpointPath = "pota/activations/";
    private DateTime? _lastLogTime;
    private int? _lastResolvedActivationId;

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

    private readonly Uri _endpointUri = BuildEndpointUri(logbookApiUrl, ForwardingEndpointPath);
    private readonly Uri _activationBaseUri = BuildEndpointUri(logbookApiUrl, ActivationsEndpointPath);

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
        activityLog?.LogWarning("[ADIF UDP] Forwarding queue full. Dropped record.");
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
        catch (Exception ex)
        {
            // The worker loop itself crashed — this means no more QSOs will be forwarded.
            logger.LogError(ex, "ADIF forwarding worker loop crashed; no further QSOs will be forwarded until restart");
            activityLog?.LogError("[ADIF UDP] Forwarding worker crashed. Restart RigCommander to resume logging.");
        }
    }

    private async Task SendWithRetryAsync(AdifForwardingItem item, CancellationToken cancellationToken)
    {
        if (TryGetBlockingProcess(out var processName))
        {
            activityLog?.LogWarning($"[ADIF UDP] LOG: Skipped API forward: '{processName}' is running.");
            return;
        }

        var activationId = activationIdStore?.Get();

        if (!AdifQsoRequestMapper.TryMap(item.Fields, activationId, out var payload, out var mappingError))
        {
            logger.LogWarning("Skipping ADIF forward because mapping failed: {Error}", mappingError);
            activityLog?.LogWarning($"[ADIF UDP] Skipped API forward: {mappingError}");
            return;
        }

        var activationIdResolved = false;

        for (var attempt = 0; attempt <= settings.MaxRetries; attempt++)
        {
            try
            {
                if (!activationIdResolved)
                {
                    activationId = await ResolveActivationIdForQsoDateAsync(activationId, payload.Qso.Date, cancellationToken);
                    payload.PotaActivationId = activationId;
                    activationIdResolved = true;
                }

                using var response = await _httpClient.PostAsJsonAsync(_endpointUri, payload, JsonSerializerOptions.Web, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _lastLogTime = payload.Qso.Date;
                    _lastResolvedActivationId = payload.PotaActivationId;
                    activityLog?.LogInformation($"[ADIF UDP] LOG: {payload.Qso.Call}, Freq: {payload.Qso.Freq}, Band: {payload.Qso.Band}, Mode: {payload.Qso.Mode}, Grid: {payload.Qso.Grid}, Cmt: {payload.Qso.Comment}");
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
                logger.LogWarning(ex, "ADIF forwarding failed on attempt {Attempt}/{Attempts}", attempt + 1, settings.MaxRetries + 1);
            }

            if (attempt >= settings.MaxRetries)
                break;

            if (settings.RetryDelayMs > 0)
                await Task.Delay(settings.RetryDelayMs, cancellationToken);
        }

        var failedCall = item.Fields.TryGetValue("CALL", out var c) ? c : "-";
        logger.LogError("ADIF forwarding failed after {Attempts} attempt(s) for call={Call}; API may be down", settings.MaxRetries + 1, failedCall);
        activityLog?.LogError($"[ADIF UDP] Failed to forward ADIF record for {failedCall} after retries. API may be down.");
    }

    private async Task<int?> ResolveActivationIdForQsoDateAsync(int? activationId, DateTime qsoDate, CancellationToken cancellationToken)
    {
        if (activationId is null)
            return null;

        if (_lastResolvedActivationId == activationId && _lastLogTime is not null && IsSameUtcDate(_lastLogTime.Value, qsoDate))
            return activationId;

        var activation = await GetActivationAsync(activationId.Value, cancellationToken);

        if (IsSameUtcDate(activation.StartDate, qsoDate))
            return activationId;

        var newActivationId = await CreateActivationForNewDayAsync(activation, qsoDate, cancellationToken);
        activationIdStore?.Set(newActivationId);

        logger.LogInformation(
            "Created new POTA activation {NewActivationId} from previous activation {PreviousActivationId} for QSO date {QsoDate:yyyy-MM-dd}",
            newActivationId,
            activation.Id,
            qsoDate);
        activityLog?.LogInformation($"[ADIF UDP] Created new POTA activation {newActivationId} for {qsoDate:yyyy-MM-dd}; forwarding QSO with the new id.");

        return newActivationId;
    }

    private async Task<PotaActivationPayload> GetActivationAsync(int activationId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(BuildActivationUri(activationId), cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<PotaActivationPayload>(JsonSerializerOptions.Web, cancellationToken)
               ?? throw new InvalidOperationException($"Activation lookup returned an empty response for id {activationId}.");
    }

    private async Task<int> CreateActivationForNewDayAsync(PotaActivationPayload previousActivation, DateTime qsoDate, CancellationToken cancellationToken)
    {
        var request = new NewActivationPayload
        {
            PrevDayActivationId = previousActivation.Id,
            ParkNumber = previousActivation.ParkNum,
            Grid = previousActivation.Grid,
            County = previousActivation.County,
            State = previousActivation.State,
            Lat = previousActivation.Lat,
            Lon = previousActivation.Long,
            StationCallsign = previousActivation.StationCallsign,
            OperatorCallsign = previousActivation.OperatorCallsign,
            StartDate = qsoDate
        };

        using var response = await _httpClient.PostAsJsonAsync(_activationBaseUri, request, JsonSerializerOptions.Web, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<int>(JsonSerializerOptions.Web, cancellationToken);
    }

    private Uri BuildActivationUri(int activationId)
        => new(_activationBaseUri, activationId.ToString(CultureInfo.InvariantCulture));

    private static bool IsSameUtcDate(DateTime left, DateTime right)
        => DateOnly.FromDateTime(left) == DateOnly.FromDateTime(right);

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

    private static Uri BuildEndpointUri(string logbookApiUrl, string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(logbookApiUrl);

        var baseUri = new Uri(logbookApiUrl, UriKind.Absolute);

        if (!baseUri.AbsolutePath.EndsWith('/'))
            baseUri = new Uri($"{baseUri.AbsoluteUri}/");

        return new Uri(baseUri, relativePath);
    }

    [SuppressMessage("Performance", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by System.Text.Json during response deserialization.")]
    private sealed record PotaActivationPayload
    {
        public int Id { get; init; }
        public DateTime StartDate { get; init; }
        public string ParkNum { get; init; } = string.Empty;
        public string Grid { get; init; } = string.Empty;
        public string County { get; init; } = string.Empty;
        public string State { get; init; } = string.Empty;
        public decimal Lat { get; init; }
        public decimal Long { get; init; }
        public string StationCallsign { get; init; } = string.Empty;
        public string OperatorCallsign { get; init; } = string.Empty;
    }

    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local", Justification = "Read by System.Text.Json during request serialization.")]
    private sealed record NewActivationPayload
    {
        public int PrevDayActivationId { get; init; }
        public string ParkNumber { get; init; } = string.Empty;
        public string Grid { get; init; } = string.Empty;
        public string County { get; init; } = string.Empty;
        public string State { get; init; } = string.Empty;
        public decimal Lat { get; init; }
        public decimal Lon { get; init; }
        public DateTime StartDate { get; init; }
        public string StationCallsign { get; init; } = string.Empty;
        public string OperatorCallsign { get; init; } = string.Empty;
    }
}
