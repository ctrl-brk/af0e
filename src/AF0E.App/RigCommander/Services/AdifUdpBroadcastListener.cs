using System.Buffers.Binary;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using AF0E.Common.Utils;
using Microsoft.Win32;
using RigCommander.Abstractions;

namespace RigCommander.Services;

#pragma warning disable CA1848 // Use the LoggerMessage delegates
#pragma warning disable CA1873

public sealed class AdifUdpBroadcastListener(
    AdifUdpSettings settings,
    ILogger<AdifUdpBroadcastListener> logger,
    IScriptActivityLog? activityLog = null,
    AdifApiForwarder? apiForwarder = null) : IDisposable
{
    private const uint WsjtxMagic = 0xADBCCBDA;
    private const int DuplicateCacheMaxEntries = 512;
    private static readonly TimeSpan _duplicateWindow = TimeSpan.FromSeconds(20);

    // Retry delays for socket recreation after a network disruption or sleep/wake cycle.
    // Bind to IPAddress.Any is fast, so short intervals are enough.
    private static readonly int[] _recreateRetryDelaysSeconds = [2, 5, 10, 15, 30];

    private UdpClient _udpClient = CreateUdpClient(settings);
    private readonly Dictionary<string, DateTime> _recentRecordFingerprints = new(StringComparer.OrdinalIgnoreCase);
    private volatile CancellationTokenSource? _socketCts;
    // Prevents multiple simultaneous reconnections when NetworkAddressChanged fires repeatedly.
    private volatile bool _reconnecting;
    private bool _started;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_started)
            return Task.CompletedTask;

        _started = true;

        if (settings.JoinMulticastGroup)
            logger.LogInformation("ADIF UDP listener joining multicast group {Group}", settings.MulticastGroup);

        logger.LogInformation("ADIF UDP listener started on UDP port {Port}", settings.Port);
        return Task.Run(() => StartWithMulticastAsync(cancellationToken), cancellationToken);
    }

    private async Task StartWithMulticastAsync(CancellationToken cancellationToken)
    {
        // Fire-and-forget: don't await the multicast join so the listen loop starts immediately
        // and can receive unicast packets while multicast routing finishes initialising.
        _ = TryJoinMulticastGroupAsync(_udpClient, cancellationToken);
        await ListenLoopAsync(cancellationToken);
    }

    private async Task ListenLoopAsync(CancellationToken cancellationToken)
    {
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
        NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                UdpReceiveResult packet;

                // Per-socket CTS: allows power/network events to abort the current ReceiveAsync
                // without cancelling the overall listener.
                var socketCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                _socketCts = socketCts;

                try
                {
                    packet = await _udpClient.ReceiveAsync(socketCts.Token);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    socketCts.Dispose();
                    break;
                }
                catch (OperationCanceledException)
                {
                    // socketCts was cancelled by a power-resume or network-change event.
                    socketCts.Dispose();
                    _socketCts = null;
                    if (!await RecreateUdpClientAsync(cancellationToken))
                        break;
                    continue;
                }
                catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
                {
                    socketCts.Dispose();
                    break;
                }
                catch (ObjectDisposedException)
                {
                    // Socket was disposed due to a failed recreation — do not spin.
                    socketCts.Dispose();
                    _socketCts = null;
                    logger.LogWarning("ADIF UDP socket was disposed unexpectedly; recreating socket");
                    if (!await RecreateUdpClientAsync(cancellationToken))
                        break;
                    continue;
                }
                catch (SocketException ex)
                {
                    // Socket became invalid (e.g. ICMP port unreachable, interface reset).
                    socketCts.Dispose();
                    _socketCts = null;
                    logger.LogWarning(ex, "ADIF UDP socket error ({ErrorCode}); recreating socket", ex.SocketErrorCode);
                    activityLog?.LogWarning($"[ADIF UDP] Socket error ({ex.SocketErrorCode}); reconnecting...");
                    if (!await RecreateUdpClientAsync(cancellationToken))
                        break;
                    continue;
                }
                catch (Exception ex)
                {
                    socketCts.Dispose();
                    _socketCts = null;
                    logger.LogWarning(ex, "ADIF UDP receive failed; listener continues running");
                    continue;
                }

                socketCts.Dispose();
                _socketCts = null;

                if (packet.Buffer.Length == 0)
                    continue;

                try
                {
                    ProcessPacket(packet.Buffer, packet.RemoteEndPoint);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "ADIF UDP packet processing failed for packet from {RemoteEndPoint}; listener continues running", packet.RemoteEndPoint);
                    activityLog?.LogError($"[ADIF UDP] Failed to process packet from {packet.RemoteEndPoint}. See log for details.");
                }
            }
        }
        finally
        {
            NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
            SystemEvents.PowerModeChanged -= OnPowerModeChanged;
            logger.LogInformation("ADIF UDP listener stopped");
        }
    }

    private void OnPowerModeChanged(object sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode != PowerModes.Resume)
            return;

        if (_reconnecting)
            return;

        logger.LogInformation("PC waking from sleep; ADIF UDP socket will be recreated");
        activityLog?.LogInformation("[ADIF UDP] PC resumed from sleep; reconnecting...");
        _socketCts?.Cancel();
    }

    private void OnNetworkAddressChanged(object? sender, EventArgs e)
    {
        // This event fires many times per network change. Ignore if we are already handling one.
        if (_reconnecting)
            return;

        logger.LogInformation("Network address changed; ADIF UDP socket will be recreated");
        activityLog?.LogInformation("[ADIF UDP] Network changed; reconnecting...");
        _socketCts?.Cancel();
    }

    /// <returns><c>true</c> if the socket was successfully recreated; <c>false</c> if all attempts
    /// failed and the listen loop should exit (the app must be restarted to resume).</returns>
    private async Task<bool> RecreateUdpClientAsync(CancellationToken cancellationToken)
    {
        _reconnecting = true;
        try
        {
            // Brief pause to let the network stack settle and suppress the flood of
            // repeated NetworkAddressChanged events that follow a single network change.
            try { await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken); }
            catch (OperationCanceledException) { return false; }

            // Explicitly close the socket at the OS level before disposing the managed wrapper
            // to avoid WSAEADDRINUSE on the very first bind attempt.
            try { _udpClient.Client.Close(); } catch { /* ignore */ }
            try { _udpClient.Dispose(); } catch { /* ignore */ }

            for (var attempt = 1; attempt <= _recreateRetryDelaysSeconds.Length + 1; attempt++)
            {
                try
                {
                    _udpClient = CreateUdpClient(settings);
                    logger.LogInformation("ADIF UDP socket recreated on port {Port} (attempt {Attempt})", settings.Port, attempt);
                    activityLog?.LogInformation($"[ADIF UDP] Reconnected on port {settings.Port}.");
                    // Fire-and-forget multicast join — don't block the listen loop resuming.
                    _ = TryJoinMulticastGroupAsync(_udpClient, cancellationToken);
                    return true;
                }
                catch (Exception ex) when (attempt <= _recreateRetryDelaysSeconds.Length)
                {
                    var delaySec = _recreateRetryDelaysSeconds[attempt - 1];
                    logger.LogWarning(ex,
                        "ADIF UDP socket recreation attempt {Attempt} failed ({ErrorType}: {ErrorMessage}); retrying in {Delay}s",
                        attempt, ex.GetType().Name, ex.Message, delaySec);
                    try { await Task.Delay(TimeSpan.FromSeconds(delaySec), cancellationToken); }
                    catch (OperationCanceledException) { return false; }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "ADIF UDP socket could not be recreated after {Attempts} attempts ({ErrorType}: {ErrorMessage}); listener will stop. Restart RigCommander to resume",
                        attempt, ex.GetType().Name, ex.Message);
                    activityLog?.LogError($"[ADIF UDP] Failed to reconnect after {attempt} attempts. Restart RigCommander to resume.");
                    return false;
                }
            }

            return false;
        }
        finally
        {
            _reconnecting = false;
        }
    }

    private void ProcessPacket(byte[] packetBytes, IPEndPoint remoteEndPoint)
    {
        if (!TryExtractAdifPayload(packetBytes, settings, out var adifPayload, out var sourceDescription, out var diagnostics))
        {
            if (settings.LogUnknownFormats)
                logger.LogDebug("Received UDP packet from {RemoteEndPoint}: {Diagnostics}", remoteEndPoint, diagnostics);
            return;
        }

        var records = AdifParser.Parse(adifPayload);

        if (records.Count == 0)
        {
            if (settings.LogUnknownFormats)
                logger.LogDebug("Received ADIF UDP payload with no records from {RemoteEndPoint}", remoteEndPoint);
            activityLog?.LogWarning($"[ADIF UDP] No ADIF records parsed from {remoteEndPoint}");
            return;
        }

        var nowUtc = DateTime.UtcNow;
        var uniqueIndexes = new List<int>(records.Count);
        var duplicateCount = 0;

        for (var index = 0; index < records.Count; index++)
        {
            var record = records[index];

            //WSJT-X sometimes sends duplicate notifications
            if (TryBuildRecordFingerprint(record.Fields, out var fingerprint) && IsDuplicateWithinWindow(fingerprint, nowUtc))
            {
                duplicateCount++;
                continue;
            }

            uniqueIndexes.Add(index);
        }

        if (uniqueIndexes.Count == 0)
        {
            activityLog?.LogDebug($"[ADIF UDP] Ignored duplicate packet from {remoteEndPoint} ({duplicateCount} duplicate record(s)).");
            return;
        }

        activityLog?.LogDebug($"[ADIF UDP] {sourceDescription}: {FormatPayloadForActivity(adifPayload)}");
        activityLog?.LogDebug($"[ADIF UDP] Parsed {uniqueIndexes.Count} record(s) from {remoteEndPoint}");

        for (var i = 0; i < uniqueIndexes.Count; i++)
        {
            var record = records[uniqueIndexes[i]];

            activityLog?.LogDebug(
                $"[ADIF UDP] #{i + 1} call={record["CALL"] ?? "-"}, band={record["BAND"] ?? "-"}, mode={record["MODE"] ?? "-"}, submode={record["SUBMODE"] ?? "-"}");

            if (apiForwarder is null)
                continue;

            var forwardItem = new AdifForwardingItem(
                nowUtc,
                sourceDescription,
                remoteEndPoint.ToString(),
                adifPayload,
                new Dictionary<string, string>(record.Fields, StringComparer.OrdinalIgnoreCase));

            _ = apiForwarder.TryEnqueue(forwardItem);
        }

        if (duplicateCount > 0)
            activityLog?.LogDebug($"[ADIF UDP] Ignored {duplicateCount} duplicate record(s) from {remoteEndPoint}.");
    }

    private static string FormatPayloadForActivity(string payload)
    {
        var normalized = payload
            .Replace("\r", "\\r", StringComparison.Ordinal)
            .Replace("\n", "\\n", StringComparison.Ordinal);

        const int maxLength = 400;
        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength] + "...";
    }

    /// <summary>
    /// Creates and binds a UDP socket. Multicast group join is intentionally excluded here —
    /// it is done separately because <c>IP_ADD_MEMBERSHIP</c> can fail with WSAEHOSTUNREACH
    /// after sleep/wake even when the bind itself succeeds, and the two operations need
    /// independent retry logic.
    /// </summary>
    private static UdpClient CreateUdpClient(AdifUdpSettings settings)
    {
        var udpClient = new UdpClient(AddressFamily.InterNetwork)
        {
            ExclusiveAddressUse = false
        };

        udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, settings.Port));

        return udpClient;
    }

    /// <summary>
    /// Attempts to join the multicast group on <paramref name="udpClient"/>. Retries a few times
    /// with short delays because <c>IP_ADD_MEMBERSHIP</c> frequently throws WSAEHOSTUNREACH right
    /// after sleep/wake while multicast routing is still initializing. If all attempts fail the
    /// socket keeps working for unicast traffic only. The <paramref name="udpClient"/> parameter
    /// is captured explicitly so that background retries don't accidentally operate on a
    /// replaced/disposed socket if the listener recreates its socket in the meantime.
    /// </summary>
    private async Task TryJoinMulticastGroupAsync(UdpClient udpClient, CancellationToken cancellationToken)
    {
        if (!settings.JoinMulticastGroup)
            return;

        var group = IPAddress.Parse(settings.MulticastGroup);
        var joined = false;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                udpClient.JoinMulticastGroup(group);

                if (!joined)
                {
                    joined = true;
                    logger.LogInformation("Joined multicast group {Group}", settings.MulticastGroup);
                }
            }
            catch (ObjectDisposedException) { return; } // socket replaced — stop
            catch (OperationCanceledException) { return; }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
            {
                // Already a member — socket is still receiving multicast packets, nothing to do.
                // No drop+rejoin: that would create a packet-loss window between the two calls.
            }
            catch
            {
                // Route not available (WSJT-X not running) — retry silently next cycle.
                joined = false;
            }

            try { await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken); }
            catch (OperationCanceledException) { return; }
        }
    }

    private static bool TryExtractAdifPayload(byte[] packetBytes, AdifUdpSettings settings, out string adifPayload, out string sourceDescription, out string diagnostics)
    {
        var packet = packetBytes.AsSpan();
        diagnostics = string.Empty;

        if (settings.AcceptWsjtxFormat && TryParseWsjtxHeader(packet, out var schema, out var messageType, out var clientId, out var payloadOffset))
        {
            if (TryExtractWsjtxAdif(packet, payloadOffset, messageType, out adifPayload, out var foundInField))
            {
                sourceDescription = $"WSJT-X schema {schema} type {messageType} (field: {foundInField}) from '{clientId}'";
                return true;
            }

            diagnostics = $"WSJT-X header detected (schema {schema}, type {messageType}, id '{clientId}') but no ADIF payload found";
            sourceDescription = string.Empty;
            return false;
        }

        if (settings.AcceptRawAdif)
        {
            var rawPayload = Encoding.UTF8.GetString(packetBytes);
            if (LooksLikeAdif(rawPayload))
            {
                adifPayload = rawPayload;
                sourceDescription = "Raw UDP ADIF";
                return true;
            }

            diagnostics = $"Payload looks like raw text but not ADIF; first {Math.Min(50, rawPayload.Length)} chars: {rawPayload[..Math.Min(50, rawPayload.Length)].Replace("\n", "\\n", StringComparison.Ordinal)}";
        }
        else if (!settings.AcceptWsjtxFormat)
        {
            diagnostics = "Neither WSJT-X nor raw ADIF formats are accepted";
        }
        else
        {
            diagnostics = "Not recognized as WSJT-X or raw ADIF format";
        }

        adifPayload = string.Empty;
        sourceDescription = string.Empty;
        return false;
    }

    private static bool TryParseWsjtxHeader(ReadOnlySpan<byte> packet, out uint schema, out uint messageType, out string clientId, out int payloadOffset)
    {
        schema = 0;
        messageType = 0;
        clientId = string.Empty;
        payloadOffset = 0;

        if (packet.Length < 16)
            return false;

        var magic = BinaryPrimitives.ReadUInt32BigEndian(packet[..4]);
        if (magic != WsjtxMagic)
            return false;

        schema = BinaryPrimitives.ReadUInt32BigEndian(packet.Slice(4, 4));
        messageType = BinaryPrimitives.ReadUInt32BigEndian(packet.Slice(8, 4));

        var offset = 12;
        if (!TryReadQtUtf8String(packet, ref offset, out clientId))
            return false;

        payloadOffset = offset;
        return true;
    }

    private static bool TryExtractWsjtxAdif(ReadOnlySpan<byte> packet, int payloadOffset, uint messageType, out string adifPayload, out string foundInField)
    {
        var offset = payloadOffset;
        foundInField = "unknown";

        // WSJT-X Logged ADIF message type.
        // Type 12: Logged ADIF
        // Type 6: QSO Logged (may contain ADIF in structured fields)
        if (messageType is 12 or 6 && TryReadQtUtf8String(packet, ref offset, out var loggedAdif) && LooksLikeAdif(loggedAdif))
        {
            adifPayload = loggedAdif;
            foundInField = messageType == 12 ? "LoggedAdif" : "QsoLogged[field0]";
            return true;
        }

        // Defensive scan for schema variation: look through a few Qt strings for ADIF text.
        offset = payloadOffset;
        for (var fieldIndex = 0; fieldIndex < 16 && offset < packet.Length; fieldIndex++)
        {
            var nextOffset = offset;
            if (!TryReadQtUtf8String(packet, ref nextOffset, out var value))
                break;

            offset = nextOffset;
            if (!LooksLikeAdif(value))
                continue;

            adifPayload = value;
            foundInField = $"field[{fieldIndex}]";
            return true;
        }

        adifPayload = string.Empty;
        return false;
    }

    private static bool TryReadQtUtf8String(ReadOnlySpan<byte> packet, ref int offset, out string value)
    {
        value = string.Empty;

        if (offset + 4 > packet.Length)
            return false;

        var length = BinaryPrimitives.ReadUInt32BigEndian(packet.Slice(offset, 4));
        offset += 4;

        switch (length)
        {
            case uint.MaxValue:
                return true;
            case > int.MaxValue:
                return false;
        }

        var intLength = (int)length;
        if (offset + intLength > packet.Length)
            return false;

        value = Encoding.UTF8.GetString(packet.Slice(offset, intLength));
        offset += intLength;
        return true;
    }

    private static bool LooksLikeAdif(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        return text.Contains('<', StringComparison.Ordinal)
               && (text.Contains("<EOR>", StringComparison.OrdinalIgnoreCase)
                   || text.Contains("<EOH>", StringComparison.OrdinalIgnoreCase)
                   || text.Contains("<CALL:", StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose()
    {
        _socketCts?.Cancel();
        _socketCts?.Dispose();
        _udpClient.Dispose();
    }

    private bool IsDuplicateWithinWindow(string fingerprint, DateTime nowUtc)
    {
        PruneDuplicateCache(nowUtc);

        if (_recentRecordFingerprints.TryGetValue(fingerprint, out var lastSeenUtc))
        {
            if (nowUtc - lastSeenUtc <= _duplicateWindow)
                return true;
        }

        _recentRecordFingerprints[fingerprint] = nowUtc;
        return false;
    }

    private void PruneDuplicateCache(DateTime nowUtc)
    {
        if (_recentRecordFingerprints.Count == 0)
            return;

        var cutoffUtc = nowUtc - _duplicateWindow;
        var expiredKeys = _recentRecordFingerprints
            .Where(kvp => kvp.Value < cutoffUtc)
            .Select(kvp => kvp.Key)
            .ToArray();

        foreach (var key in expiredKeys)
            _recentRecordFingerprints.Remove(key);

        if (_recentRecordFingerprints.Count <= DuplicateCacheMaxEntries)
            return;

        var overflowKeys = _recentRecordFingerprints
            .OrderBy(kvp => kvp.Value)
            .Take(_recentRecordFingerprints.Count - DuplicateCacheMaxEntries)
            .Select(kvp => kvp.Key)
            .ToArray();

        foreach (var key in overflowKeys)
            _recentRecordFingerprints.Remove(key);
    }

    private static bool TryBuildRecordFingerprint(IReadOnlyDictionary<string, string> fields, out string fingerprint)
    {
        fingerprint = string.Empty;

        if (!TryGetNormalizedField(fields, "CALL", out var call)
            || !TryGetNormalizedField(fields, "QSO_DATE", out var qsoDate)
            || !TryGetNormalizedAdifTime(fields, "TIME_ON", out var timeOn)
            || !TryGetNormalizedField(fields, "BAND", out var band))
        {
            return false;
        }

        var mode = GetNormalizedField(fields, "SUBMODE")
                   ?? GetNormalizedField(fields, "MODE")
                   ?? "-";

        fingerprint = $"{call}|{qsoDate}|{timeOn}|{band}|{mode}";
        return true;
    }

    private static bool TryGetNormalizedAdifTime(IReadOnlyDictionary<string, string> fields, string key, out string value)
    {
        value = string.Empty;

        if (!fields.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
            return false;

        var digits = new string(raw.Where(char.IsDigit).ToArray());
        if (digits.Length < 4)
            return false;

        // ADIF times may be HHMM or HHMMSS; normalize to HHMMSS for stable dedupe keys.
        value = digits.Length >= 6 ? digits[..6] : digits[..4] + "00";
        return true;
    }

    private static bool TryGetNormalizedField(IReadOnlyDictionary<string, string> fields, string key, out string value)
    {
        value = string.Empty;

        if (!fields.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
            return false;

        value = raw.Trim().ToUpperInvariant();
        return value.Length > 0;
    }

    private static string? GetNormalizedField(IReadOnlyDictionary<string, string> fields, string key)
        => fields.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw)
            ? raw.Trim().ToUpperInvariant()
            : null;
}

