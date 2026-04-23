using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Text;
using AF0E.Common.Utils;
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
    private readonly UdpClient _udpClient = CreateUdpClient(settings);
    private bool _started;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_started)
            return Task.CompletedTask;

        _started = true;

        if (settings.JoinMulticastGroup)
            logger.LogInformation("ADIF UDP listener joining multicast group {Group}", settings.MulticastGroup);

        logger.LogInformation("ADIF UDP listener started on UDP port {Port}", settings.Port);
        return Task.Run(() => ListenLoopAsync(cancellationToken), cancellationToken);
    }

    private async Task ListenLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            UdpReceiveResult packet;

            try
            {
                packet = await _udpClient.ReceiveAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "ADIF UDP receive failed; listener continues running");
                continue;
            }

            if (packet.Buffer.Length == 0)
                continue;

            ProcessPacket(packet.Buffer, packet.RemoteEndPoint);
        }

        logger.LogInformation("ADIF UDP listener stopped");
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
        activityLog?.AppendLine($"[ADIF UDP] {sourceDescription}: {FormatPayloadForActivity(adifPayload)}");

        if (records.Count == 0)
        {
            if (settings.LogUnknownFormats)
                logger.LogDebug("Received ADIF UDP payload with no records from {RemoteEndPoint}", remoteEndPoint);
            activityLog?.AppendLine($"[ADIF UDP] No ADIF records parsed from {remoteEndPoint}");
            return;
        }

        activityLog?.AppendLine($"[ADIF UDP] Parsed {records.Count} record(s) from {remoteEndPoint}");

        for (var index = 0; index < records.Count; index++)
        {
            var record = records[index];
            activityLog?.AppendLine(
                $"[ADIF UDP] #{index + 1} call={record["CALL"] ?? "-"}, band={record["BAND"] ?? "-"}, mode={record["MODE"] ?? "-"}, submode={record["SUBMODE"] ?? "-"}");

            if (apiForwarder is null)
                continue;

            var forwardItem = new AdifForwardingItem(
                DateTime.UtcNow,
                sourceDescription,
                remoteEndPoint.ToString(),
                adifPayload,
                new Dictionary<string, string>(record.Fields, StringComparer.OrdinalIgnoreCase));

            _ = apiForwarder.TryEnqueue(forwardItem);
        }
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

    private static UdpClient CreateUdpClient(AdifUdpSettings settings)
    {
        var udpClient = new UdpClient(AddressFamily.InterNetwork)
        {
            ExclusiveAddressUse = false
        };

        udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, settings.Port));

        if (settings.JoinMulticastGroup)
            udpClient.JoinMulticastGroup(IPAddress.Parse(settings.MulticastGroup));

        return udpClient;
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
        _udpClient.Dispose();
    }
}
