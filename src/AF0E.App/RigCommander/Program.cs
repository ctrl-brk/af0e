using System.IO.Ports;
using System.Net.Mime;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

// Keep JSON predictable
builder.Services.Configure<JsonOptions>(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = null;
});

var app = builder.Build();

app.UseCors("AllowAll");

app.MapGet("/health", () => Results.Ok(new { ok = true }));

// Configure your CI-V connection here (or move to appsettings.json later)
var civ = new CivIcomSerial(
    portName: "COM3",
    baudRate: 19200,
    radioAddress: 0x7C,
    controllerAddress: 0xE0
);

// Open on startup; you can also lazy-open per request if you prefer
civ.Open();

app.Lifetime.ApplicationStopping.Register(() =>
{
    civ.Dispose();
});

app.MapPost("/radio/frequency", (SetFrequencyRequest req) =>
{
    if (req.FrequencyHz <= 0 || req.FrequencyHz > 3_000_000_000L)
        return Results.BadRequest(new { error = "FrequencyHz must be a positive Hz value in a reasonable range." });

    civ.SetFrequency(req.FrequencyHz);

    var status = civ.GetStatus();

    return Results.Ok(new
    {
        ok = true,
        applied = new { frequencyHz = req.FrequencyHz },
        current = new
        {
            frequencyHz = status.FrequencyHz,
            mode = status.DisplayMode,
            filter = status.Filter,
            data = status.DataModeOn
        }
    });
})
.Accepts<SetFrequencyRequest>(MediaTypeNames.Application.Json)
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

app.MapPost("/radio/mode", (SetModeRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.Mode))
        return Results.BadRequest(new { error = "Mode is required." });

    // Parse the same way you do in /radio/status:
    IcomMode? baseMode = null;
    bool? dataOn = null;

    var raw = req.Mode.Trim();

    var isUsbD = raw.Equals("USB-D", StringComparison.OrdinalIgnoreCase) ||
                 raw.Equals("USBD", StringComparison.OrdinalIgnoreCase) ||
                 raw.Equals("FT8", StringComparison.OrdinalIgnoreCase) ||
                 raw.Equals("FT4", StringComparison.OrdinalIgnoreCase) ||
                 raw.Equals("FT2", StringComparison.OrdinalIgnoreCase);
    var isLsbD = raw.Equals("LSB-D", StringComparison.OrdinalIgnoreCase) ||
                 raw.Equals("LSBD", StringComparison.OrdinalIgnoreCase);

    if (isUsbD) { baseMode = IcomMode.USB; dataOn = true; }
    else if (isLsbD) { baseMode = IcomMode.LSB; dataOn = true; }
    else
    {
        // Traditional behavior: specifying a non -D mode turns DATA off
        dataOn = false;

        if (!Enum.TryParse<IcomMode>(raw, ignoreCase: true, out var parsed))
            return Results.BadRequest(new { error = "Unsupported Mode. Try: LSB, USB, CW, AM, FM, WFM, RTTY, RTTYR, USB-D, LSB-D, FT8, FT4, FT2" });

        baseMode = parsed;
    }

    civ.SetMode(baseMode.Value);
    civ.SetDataMode(dataOn.Value);

    var status = civ.GetStatus();

    return Results.Ok(new
    {
        ok = true,
        applied = new { mode = req.Mode },
        current = new
        {
            frequencyHz = status.FrequencyHz,
            mode = status.DisplayMode,
            filter = status.Filter,
            data = status.DataModeOn
        }
    });
})
.Accepts<SetModeRequest>(MediaTypeNames.Application.Json)
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

app.MapPost("/radio/status", (SetStatusRequest req) =>
{
    if (req.FrequencyHz is null && string.IsNullOrWhiteSpace(req.Mode))
        return Results.BadRequest(new { error = "Provide FrequencyHz and/or Mode." });

    // Parse mode (supports USB-D / LSB-D)
    IcomMode? baseMode = null;
    bool? dataOn = null;

    if (!string.IsNullOrWhiteSpace(req.Mode))
    {
        var raw = req.Mode.Trim();

        // accept "USB-D" or "LSB-D" (also tolerate "USBD"/"LSBD")
        var isUsbD = raw.Equals("USB-D", StringComparison.OrdinalIgnoreCase) ||
                     raw.Equals("USBD", StringComparison.OrdinalIgnoreCase) ||
                     raw.Equals("FT8", StringComparison.OrdinalIgnoreCase) ||
                     raw.Equals("FT4", StringComparison.OrdinalIgnoreCase) ||
                     raw.Equals("FT2", StringComparison.OrdinalIgnoreCase);
        var isLsbD = raw.Equals("LSB-D", StringComparison.OrdinalIgnoreCase) ||
                     raw.Equals("LSBD", StringComparison.OrdinalIgnoreCase);

        if (isUsbD) { baseMode = IcomMode.USB; dataOn = true; }
        else if (isLsbD) { baseMode = IcomMode.LSB; dataOn = true; }
        else
        {
            // If user sets a non -D mode, treat as “DATA off” (traditional expectation)
            dataOn = false;

            if (!Enum.TryParse<IcomMode>(raw, ignoreCase: true, out var parsed))
                return Results.BadRequest(new { error = "Unsupported Mode. Try: LSB, USB, CW, AM, FM, WFM, RTTY, RTTYR, USB-D, LSB-D, FT8, FT4, FT2" });

            baseMode = parsed;
        }
    }

    // Apply changes atomically so serial frames don’t interleave with other requests
    lock (civ.SyncRoot)
    {
        if (req.FrequencyHz is not null)
        {
            if (req.FrequencyHz <= 0 || req.FrequencyHz > 3_000_000_000L)
                return Results.BadRequest(new { error = "FrequencyHz must be a positive Hz value in a reasonable range." });

            civ.SetFrequency(req.FrequencyHz.Value);
        }

        if (baseMode is not null)
            civ.SetMode(baseMode.Value);

        if (dataOn is not null)
            civ.SetDataMode(dataOn.Value);
    }

    // Return readback (nice for clients)
    var status = civ.GetStatus();

    return Results.Ok(new
    {
        ok = true,
        applied = new
        {
            req.FrequencyHz,
            req.Mode
        },
        current = new
        {
            frequencyHz = status.FrequencyHz,
            mode = status.DisplayMode,     // <-- USB-D aware
            filter = status.Filter,
            data = status.DataModeOn       // <-- explicit
        }
    });
})
.Accepts<SetStatusRequest>(MediaTypeNames.Application.Json)
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest);

app.MapGet("/radio/mode", () =>
{
    var modeStatus = civ.GetMode();         // 0x04 -> mode + filter slot
    var dataStatus = civ.GetDataMode();     // 1A 06 -> data flag (+ its filter slot)

    var modeText = modeStatus.Mode.ToString();

    if (dataStatus.DataOn)
    {
        if (modeStatus.Mode == IcomMode.USB) modeText = "USB-D";
        else if (modeStatus.Mode == IcomMode.LSB) modeText = "LSB-D";
    }

    return Results.Ok(new
    {
        ok = true,
        mode = modeText,
        filter = modeStatus.Filter,
        data = dataStatus.DataOn
    });
});

app.MapGet("/radio/frequency", () =>
{
    var hz = civ.GetFrequency();

    return Results.Ok(new
    {
        ok = true,
        frequencyHz = hz
    });
});

app.MapGet("/radio/status", () =>
{
    var status = civ.GetStatus();

    return Results.Ok(new
    {
        ok = true,
        frequencyHz = status.FrequencyHz,
        mode = status.DisplayMode,
        filter = status.Filter,
        data = status.DataModeOn
    });
});

app.Run("http://0.0.0.0:5050");


// -----------------------------
// DTOs
// -----------------------------
public sealed record SetFrequencyRequest(long FrequencyHz);
public sealed record SetModeRequest(string Mode);
public sealed record SetStatusRequest(long? FrequencyHz, string? Mode);
public sealed record ModeStatus(IcomMode Mode, byte Filter);
public sealed record DataModeStatus(bool DataOn, byte FilterSlot);
public sealed record RadioStatus(long FrequencyHz, IcomMode Mode, byte Filter, bool DataModeOn)
{
    public string DisplayMode => DataModeOn && (Mode == IcomMode.USB || Mode == IcomMode.LSB) ? $"{Mode}-D" : Mode.ToString();
}


// -----------------------------
// CI-V implementation
// -----------------------------
public enum IcomMode : byte
{
    LSB = 0x00,
    USB = 0x01,
    AM = 0x02,
    CW = 0x03,
    RTTY = 0x04,
    FM = 0x05,
    WFM = 0x06,
    CW_R = 0x07,
    RTTYR = 0x08
}

public sealed class CivIcomSerial : IDisposable
{
    private readonly SerialPort _port;
    private readonly byte _radioAddress;
    private readonly byte _controllerAddress;
    private readonly object _lock = new();
    public object SyncRoot => _lock;

    public CivIcomSerial(string portName, int baudRate, byte radioAddress, byte controllerAddress)
    {
        _radioAddress = radioAddress;
        _controllerAddress = controllerAddress;

        _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
        {
            Handshake = Handshake.None,
            ReadTimeout = 500,
            WriteTimeout = 500
        };
    }

    public void Open()
    {
        lock (_lock)
        {
            if (_port.IsOpen) return;
            _port.Open();
        }
    }

    public void SetFrequency(long frequencyHz)
    {
        // CI-V "Set frequency" command: 0x05
        // Frequency is sent as 5 bytes BCD, least-significant byte first.
        // Example: 145,500,000 Hz -> BCD bytes (LSB first) for "145500000"
        var freqBcd = EncodeFrequencyBcd5(frequencyHz);

        var payload = new byte[1 + 5];
        payload[0] = 0x05;
        Array.Copy(freqBcd, 0, payload, 1, 5);

        SendFrame(payload);
    }

    public void SetMode(IcomMode mode, byte filter = 0x01)
    {
        // CI-V "Set mode" command: 0x06
        // Payload: 0x06 <mode> <filter>
        // filter is typically 0x01..0x03 depending on rig; 0x01 is a safe default.
        SendFrame([0x06, (byte)mode, filter]);
    }

    public void SetDataMode(bool enabled, byte filter = 0x01)
    {
        // Common Icom pattern: 1A 06 <data> <filter>
        // data: 00=OFF, 01=ON
        // filter: 01..03 (use 01 as safe default)
        SendFrame([0x1A, 0x06, (byte)(enabled ? 0x01 : 0x00), filter]);
    }

    public ModeStatus GetMode()
    {
        // Read operating mode: 0x04, reply payload: 0x04 <mode> <filter>
        var payload = QueryFrameExpecting(
            requestPayload: [0x04],
            predicate: p => p.Length >= 3 && p[0] == 0x04
        );

        var mode = (IcomMode)payload.Span[1];
        var filter = payload.Span[2];

        return new ModeStatus(mode, filter);
    }

    public DataModeStatus GetDataMode()
    {
        // CI-V: DATA mode setting is commonly handled by command 1A 06 (send/read). :contentReference[oaicite:1]{index=1}
        // Reply payload is typically: 1A 06 <dataMode> <filterSlot>
        // dataMode: 00 = OFF, non-zero = ON (DATA1/DATA2/DATA3 depending on rig)
        // filterSlot: 01..03

        var payload = QueryFrameExpecting(
            requestPayload: [0x1A, 0x06],
            predicate: p => p.Length >= 4 && p[0] == 0x1A && p[1] == 0x06
        );

        var dataMode = payload.Span[2];
        var filterSlot = payload.Span[3];

        return new DataModeStatus(DataOn: dataMode != 0x00, FilterSlot: filterSlot);
    }

    public long GetFrequency()
    {
        // CI-V Read Frequency command = 0x03
        // Expected reply payload:
        // 0x03 <5 bytes BCD frequency (LSB-first)>

        var payload = QueryFrameExpecting(
            requestPayload: [0x03],
            predicate: p => p.Length >= 6 && p[0] == 0x03
        );

        // Bytes 1..5 are BCD frequency bytes (LSB-first)
        return DecodeFrequencyBcd5(payload.Span.Slice(1, 5));
    }

    public RadioStatus GetStatus()
    {
        lock (_lock)
        {
            var freqPayload = QueryFrameExpecting(
                requestPayload: [0x03],
                predicate: p => p.Length >= 6 && p[0] == 0x03
            );

            var modePayload = QueryFrameExpecting(
                requestPayload: [0x04],
                predicate: p => p.Length >= 3 && p[0] == 0x04
            );

            var dataPayload = QueryFrameExpecting(
                requestPayload: [0x1A, 0x06],
                predicate: p => p.Length >= 4 && p[0] == 0x1A && p[1] == 0x06
            );

            var hz = DecodeFrequencyBcd5(freqPayload.Span.Slice(1, 5));

            var mode = (IcomMode)modePayload.Span[1];
            var filter = modePayload.Span[2];

            var dataOn = dataPayload.Span[2] != 0x00;

            return new RadioStatus(
                FrequencyHz: hz,
                Mode: mode,
                Filter: filter,
                DataModeOn: dataOn
            );
        }
    }

    private void SendFrame(ReadOnlySpan<byte> payload)
    {
        // Frame format:
        // FE FE <to> <from> <payload...> FD
        Span<byte> frame = stackalloc byte[2 + 2 + payload.Length + 1];
        frame[0] = 0xFE;
        frame[1] = 0xFE;
        frame[2] = _radioAddress;
        frame[3] = _controllerAddress;

        payload.CopyTo(frame.Slice(4));
        frame[^1] = 0xFD;

        lock (_lock)
        {
            if (!_port.IsOpen) _port.Open();
            _port.Write(frame.ToArray(), 0, frame.Length);
        }
    }

    private ReadOnlyMemory<byte> QueryFrameExpecting(
        ReadOnlySpan<byte> requestPayload,
        Func<ReadOnlySpan<byte>, bool> predicate,
        int overallTimeoutMs = 900)
    {
        lock (_lock)
        {
            if (!_port.IsOpen) _port.Open();

            // On USB, clearing stale bytes helps a lot.
            _port.DiscardInBuffer();
        }

        SendFrame(requestPayload);

        var start = Environment.TickCount;

        while (Environment.TickCount - start < overallTimeoutMs)
        {
            var frame = ReadFrame(timeoutMs: 250); // shorter “chunk” timeout, loop overall

            if (!TryExtractPayload(frame, out var payload))
                continue;

            if (predicate(payload.Span))
                return payload;
        }

        throw new TimeoutException("Timed out waiting for the expected CI-V reply.");
    }

    private bool TryExtractPayload(ReadOnlyMemory<byte> frame, out ReadOnlyMemory<byte> payload)
    {
        payload = default;

        if (frame.Length < 6) return false;
        var s = frame.Span;

        if (s[0] != 0xFE || s[1] != 0xFE || s[^1] != 0xFD)
            return false;

        var to = s[2];
        var from = s[3];

        // We only care about frames from the radio to us.
        if (from != _radioAddress || to != _controllerAddress)
            return false;

        // Payload bytes are between addresses and trailing FD.
        // Layout: FE FE to from [payload...] FD
        payload = frame[4..^1];
        return true;
    }

    private ReadOnlyMemory<byte> ReadFrame(int timeoutMs)
    {
        var start = Environment.TickCount;
        var buf = new List<byte>(64);

        // Find FE FE
        while (Environment.TickCount - start < timeoutMs)
        {
            int b;
            lock (_lock)
            {
                if (_port.BytesToRead == 0)
                {
                    Thread.Sleep(5);
                    continue;
                }
                b = _port.ReadByte();
            }

            if (b < 0) continue;
            var by = (byte)b;

            if (buf.Count == 0)
            {
                if (by == 0xFE) buf.Add(by);
                continue;
            }

            if (buf.Count == 1)
            {
                if (by == 0xFE) buf.Add(by);
                else buf.Clear();
                continue;
            }

            buf.Add(by);

            // End of frame
            if (by == 0xFD && buf.Count >= 6)
                return buf.ToArray();

            // Safety cap
            if (buf.Count > 256)
            {
                buf.Clear();
                // resync by continuing to hunt for FE FE again
            }
        }

        throw new TimeoutException("Timed out waiting for a CI-V frame.");
    }

    private static byte[] EncodeFrequencyBcd5(long frequencyHz)
    {
        // Convert Hz to decimal digits (no separators), then pack into 5 BCD bytes LSB-first.
        // CI-V uses 10 digits max in this 5-byte form.
        // We’ll clamp/pad to 10 digits.
        var digits = frequencyHz.ToString();
        if (digits.Length > 10)
            digits = digits[^10..]; // keep least significant 10 digits

        digits = digits.PadLeft(10, '0');

        // Pack two digits per byte, but send least significant byte first.
        // Example digits: "0145500000" => bytes for pairs: 01 45 50 00 00
        // LSB-first => 00 00 00 50 45? Wait carefully:
        // pairs from left: [01][45][50][00][00]
        // LSB-first means reverse pair order: [00][00][00][50][45]? That would drop the "01".
        // Correct approach: pairs represent most->least significant in left order; sending LSB-first reverses them.
        // So the 5 bytes are built from the pairs right-to-left.
        var bcd = new byte[5];
        for (int i = 0; i < 5; i++)
        {
            // take pairs from the right end
            int pairStart = digits.Length - 2 * (i + 1);
            int hi = digits[pairStart] - '0';
            int lo = digits[pairStart + 1] - '0';
            bcd[i] = (byte)((hi << 4) | lo);
        }
        return bcd;
    }

    private static long DecodeFrequencyBcd5(ReadOnlySpan<byte> bcd5)
    {
        if (bcd5.Length != 5)
            throw new ArgumentException("Expected exactly 5 BCD bytes.", nameof(bcd5));

        // Bytes are least-significant first. Each byte holds two digits: high nibble then low nibble.
        // We rebuild digits from most-significant to least-significant by iterating reversed.
        Span<char> digits = stackalloc char[10];
        int idx = 0;

        for (int i = 4; i >= 0; i--)
        {
            byte b = bcd5[i];
            int hi = (b >> 4) & 0xF;
            int lo = b & 0xF;

            if (hi > 9 || lo > 9)
                throw new FormatException($"Invalid BCD digit in byte 0x{b:X2}.");

            digits[idx++] = (char)('0' + hi);
            digits[idx++] = (char)('0' + lo);
        }

        // Parse as long; leading zeros are fine
        return long.Parse(digits);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_port.IsOpen) _port.Close();
            _port.Dispose();

        }
    }
}
