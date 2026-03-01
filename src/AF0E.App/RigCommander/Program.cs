using System.IO.Ports;
using System.Net.Mime;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Json;

#pragma warning disable CA1050

var builder = WebApplication.CreateBuilder(args);

// CORS: allow all (be careful if you ever expose beyond localhost/LAN)
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

// Global exception handler: never terminate the server for request exceptions.
// Converts uncaught exceptions into JSON 500 responses.
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        var feature = context.Features.Get<IExceptionHandlerFeature>();
        if (feature?.Error is not null)
        {
            Console.WriteLine($"[HTTP] Unhandled exception: {feature.Error}");
        }

        context.Response.StatusCode = StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            ok = false,
            error = "Internal server error"
        });
    });
});

app.UseStatusCodePages(async ctx =>
{
    // Ensure non-success codes still return JSON
    ctx.HttpContext.Response.ContentType = "application/json";
    await ctx.HttpContext.Response.WriteAsJsonAsync(new
    {
        ok = false,
        status = ctx.HttpContext.Response.StatusCode
    });
});

app.UseCors("AllowAll");

app.MapGet("/health", () => Results.Ok(new { ok = true }));

// Configure a CI-V connection here (or move to appsettings.json later)
#pragma warning disable CA2000
var civ = new CivIcomSerial(portName: "COM3", baudRate: 19200, radioAddress: 0x7C, controllerAddress: 0xE0);
#pragma warning restore CA2000

// IMPORTANT: do NOT open on startup; open/close per request
app.Lifetime.ApplicationStopping.Register(() =>
{
    Console.WriteLine("Shutting down RigCommander...");
    civ.Dispose();
});

app.MapPost("/radio/frequency", (SetFrequencyRequest req) =>
{
    if (req.FrequencyHz is <= 0 or > 3_000_000_000L)
        return Results.BadRequest(new { ok = false, error = "FrequencyHz must be a positive Hz value in a reasonable range." });

    try
    {
        var status = civ.WithConnection(() =>
        {
            civ.SetFrequency_NoScope(req.FrequencyHz);
            return civ.GetStatus_NoScope();
        });

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
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[/radio/frequency POST] {ex}");
        return Results.Json(
            new { ok = false, error = "Radio unavailable (CI-V communication failure)" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.Accepts<SetFrequencyRequest>(MediaTypeNames.Application.Json)
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status503ServiceUnavailable);

app.MapPost("/radio/mode", (SetModeRequest req) =>
{
    if (string.IsNullOrWhiteSpace(req.Mode))
        return Results.BadRequest(new { ok = false, error = "Mode is required." });

    // Parse mode (supports USB-D / LSB-D + FT8/FT4/FT2 synonyms -> USB-D)
    IcomMode? baseMode;
    bool? dataOn;

    var raw = req.Mode.Trim();

    var isUsbD = raw.Equals("USB-D", StringComparison.OrdinalIgnoreCase) ||
                 raw.Equals("USBD", StringComparison.OrdinalIgnoreCase) ||
                 raw.Equals("FT8", StringComparison.OrdinalIgnoreCase) ||
                 raw.Equals("FT4", StringComparison.OrdinalIgnoreCase) ||
                 raw.Equals("FT2", StringComparison.OrdinalIgnoreCase);

    var isLsbD = raw.Equals("LSB-D", StringComparison.OrdinalIgnoreCase) ||
                 raw.Equals("LSBD", StringComparison.OrdinalIgnoreCase);

    if (isUsbD)
    {
        baseMode = IcomMode.USB;
        dataOn = true;
    }
    else if (isLsbD)
    {
        baseMode = IcomMode.LSB;
        dataOn = true;
    }
    else
    {
        // Traditional behavior: specifying a non -D mode turns DATA off
        dataOn = false;

        if (!Enum.TryParse<IcomMode>(raw, ignoreCase: true, out var parsed))
            return Results.BadRequest(new { ok = false, error = "Unsupported Mode. Try: LSB, USB, CW, AM, FM, WFM, RTTY, RTTYR, USB-D, LSB-D, FT8, FT4, FT2" });

        baseMode = parsed;
    }

    try
    {
        var status = civ.WithConnection(() =>
        {
            civ.SetMode_NoScope(baseMode.Value);
            civ.SetDataMode_NoScope(dataOn.Value);
            return civ.GetStatus_NoScope();
        });

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
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[/radio/mode POST] {ex}");
        return Results.Json(
            new { ok = false, error = "Radio unavailable (CI-V communication failure)" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.Accepts<SetModeRequest>(MediaTypeNames.Application.Json)
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status503ServiceUnavailable);

app.MapPost("/radio/status", (SetStatusRequest req) =>
{
    if (req.FrequencyHz is null && string.IsNullOrWhiteSpace(req.Mode))
        return Results.BadRequest(new { ok = false, error = "Provide FrequencyHz and/or Mode." });

    if (req.FrequencyHz is not null && (req.FrequencyHz <= 0 || req.FrequencyHz > 3_000_000_000L))
        return Results.BadRequest(new { ok = false, error = "FrequencyHz must be a positive Hz value in a reasonable range." });

    // Parse mode (supports USB-D / LSB-D + FT8/FT4/FT2 synonyms -> USB-D)
    IcomMode? baseMode = null;
    bool? dataOn = null;

    if (!string.IsNullOrWhiteSpace(req.Mode))
    {
        var raw = req.Mode.Trim();

        var isUsbD = raw.Equals("USB-D", StringComparison.OrdinalIgnoreCase) ||
                     raw.Equals("USBD", StringComparison.OrdinalIgnoreCase) ||
                     raw.Equals("FT8", StringComparison.OrdinalIgnoreCase) ||
                     raw.Equals("FT4", StringComparison.OrdinalIgnoreCase) ||
                     raw.Equals("FT2", StringComparison.OrdinalIgnoreCase);

        var isLsbD = raw.Equals("LSB-D", StringComparison.OrdinalIgnoreCase) ||
                     raw.Equals("LSBD", StringComparison.OrdinalIgnoreCase);

        if (isUsbD)
        {
            baseMode = IcomMode.USB;
            dataOn = true;
        }
        else if (isLsbD)
        {
            baseMode = IcomMode.LSB;
            dataOn = true;
        }
        else
        {
            // Traditional behavior: specifying a non -D mode turns DATA off
            dataOn = false;

            if (!Enum.TryParse<IcomMode>(raw, ignoreCase: true, out var parsed))
                return Results.BadRequest(new { ok = false, error = "Unsupported Mode. Try: LSB, USB, CW, AM, FM, WFM, RTTY, RTTYR, USB-D, LSB-D, FT8, FT4, FT2" });

            baseMode = parsed;
        }
    }

    try
    {
        var status = civ.WithConnection(() =>
        {
            if (req.FrequencyHz is not null)
                civ.SetFrequency_NoScope(req.FrequencyHz.Value);

            if (baseMode is not null)
                civ.SetMode_NoScope(baseMode.Value);

            if (dataOn is not null)
                civ.SetDataMode_NoScope(dataOn.Value);

            return civ.GetStatus_NoScope();
        });

        return Results.Ok(new
        {
            ok = true,
            applied = new { req.FrequencyHz, req.Mode },
            current = new
            {
                frequencyHz = status.FrequencyHz,
                mode = status.DisplayMode,
                filter = status.Filter,
                data = status.DataModeOn
            }
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[/radio/status POST] {ex}");
        return Results.Json(
            new { ok = false, error = "Radio unavailable (CI-V communication failure)" },
            statusCode: StatusCodes.Status503ServiceUnavailable);
    }
})
.Accepts<SetStatusRequest>(MediaTypeNames.Application.Json)
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status503ServiceUnavailable);

app.MapGet("/radio/mode", () =>
{
    try
    {
        var status = civ.WithConnection(() => civ.GetStatus_NoScope());

        return Results.Ok(new
        {
            ok = true,
            mode = status.DisplayMode,
            filter = status.Filter,
            data = status.DataModeOn
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[/radio/mode GET] {ex}");
        return Results.Json(
            new { ok = false, error = "Radio unavailable (CI-V communication failure)" },
            statusCode: StatusCodes.Status503ServiceUnavailable);    }
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status503ServiceUnavailable);

app.MapGet("/radio/frequency", () =>
{
    try
    {
        var hz = civ.WithConnection(() => civ.GetFrequency_NoScope());

        return Results.Ok(new
        {
            ok = true,
            frequencyHz = hz
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[/radio/frequency GET] {ex}");
        return Results.Json(
            new { ok = false, error = "Radio unavailable (CI-V communication failure)" },
            statusCode: StatusCodes.Status503ServiceUnavailable);    }
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status503ServiceUnavailable);

app.MapGet("/radio/status", () =>
{
    try
    {
        var status = civ.WithConnection(() => civ.GetStatus_NoScope());

        return Results.Ok(new
        {
            ok = true,
            frequencyHz = status.FrequencyHz,
            mode = status.DisplayMode,
            filter = status.Filter,
            data = status.DataModeOn
        });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[/radio/status GET] {ex}");
        return Results.Json(
            new { ok = false, error = "Radio unavailable (CI-V communication failure)" },
            statusCode: StatusCodes.Status503ServiceUnavailable);    }
})
.Produces(StatusCodes.Status200OK)
.Produces(StatusCodes.Status503ServiceUnavailable);

app.Run("http://0.0.0.0:5050");


// DTOs
// ReSharper disable ClassNeverInstantiated.Global
public sealed record SetFrequencyRequest(long FrequencyHz);
public sealed record SetModeRequest(string Mode);
public sealed record SetStatusRequest(long? FrequencyHz, string? Mode);

public sealed record RadioStatus(long FrequencyHz, IcomMode Mode, byte Filter, bool DataModeOn)
{
    public string DisplayMode =>
        DataModeOn && (Mode == IcomMode.USB || Mode == IcomMode.LSB)
            ? $"{Mode}-D"
            : Mode.ToString();
}
// ReSharper enable ClassNeverInstantiated.Global


// CI-V implementation
#pragma warning disable CA1028
public enum IcomMode : byte
#pragma warning restore CA1028
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

public sealed class CivIcomSerial(string portName, int baudRate, byte radioAddress, byte controllerAddress) : IDisposable
{
    private readonly SerialPort _port = new(portName, baudRate, Parity.None, 8, StopBits.One)
    {
        Handshake = Handshake.None,
        ReadTimeout = 500,
        WriteTimeout = 500
    };

    /// <summary>
    /// Guards serial access so frames don't interleave. WithConnection holds this lock for the whole operation.
    /// </summary>
    private object SyncRoot { get; } = new();

    /// <summary>
    /// Open the port for the duration of <paramref name="action"/> and always close it.
    /// Exceptions are logged to the console; the caller decides how to translate to HTTP response.
    /// </summary>
    public T WithConnection<T>(Func<T> action)
    {
        lock (SyncRoot)
        {
            try
            {
                if (!_port.IsOpen)
                    _port.Open();

                // Helps on USB if stale/unsolicited bytes are buffered.
                _port.DiscardInBuffer();

                return action();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CI-V] Error: {ex}");
                throw;
            }
            finally
            {
                try
                {
                    if (_port.IsOpen)
                        _port.Close();
                }
                catch (Exception ex)
                {
                    // Never let close failures crash request handling.
                    Console.WriteLine($"[CI-V] Error closing port: {ex}");
                }
            }
        }
    }

    // -------------------------
    // NoScope operations
    // (assume: port is open and SyncRoot is held)
    // -------------------------

    public void SetFrequency_NoScope(long frequencyHz)
    {
        var freqBcd = EncodeFrequencyBcd5(frequencyHz);

        Span<byte> payload = stackalloc byte[1 + 5];
        payload[0] = 0x05;
        freqBcd.CopyTo(payload.Slice(1, 5));

        SendFrame_NoScope(payload);
    }

    public void SetMode_NoScope(IcomMode mode, byte filter = 0x01)
    {
        SendFrame_NoScope([0x06, (byte)mode, filter]);
    }

    public void SetDataMode_NoScope(bool enabled, byte filter = 0x01)
    {
        // 1A 06 <data> <filter>
        SendFrame_NoScope([0x1A, 0x06, (byte)(enabled ? 0x01 : 0x00), filter]);
    }

    public long GetFrequency_NoScope()
    {
        var payload = QueryFrameExpecting_NoScope(
            requestPayload: [0x03],
            predicate: p => p.Length >= 6 && p[0] == 0x03
        );

        return DecodeFrequencyBcd5(payload.Span.Slice(1, 5));
    }

    public RadioStatus GetStatus_NoScope()
    {
        // Frequency (0x03)
        var freqPayload = QueryFrameExpecting_NoScope(
            requestPayload: [0x03],
            predicate: p => p.Length >= 6 && p[0] == 0x03
        );

        // Mode + filter (0x04)
        var modePayload = QueryFrameExpecting_NoScope(
            requestPayload: [0x04],
            predicate: p => p.Length >= 3 && p[0] == 0x04
        );

        // Data mode flag (1A 06)
        var dataPayload = QueryFrameExpecting_NoScope(
            requestPayload: [0x1A, 0x06],
            predicate: p => p.Length >= 4 && p[0] == 0x1A && p[1] == 0x06
        );

        var hz = DecodeFrequencyBcd5(freqPayload.Span.Slice(1, 5));
        var mode = (IcomMode)modePayload.Span[1];
        var filter = modePayload.Span[2];

        // Typical: 1A 06 <data> <filter>
        // If your IC-9100 ever reports swapped bytes, swap indices.
        var dataOn = dataPayload.Span[2] != 0x00;

        return new RadioStatus(hz, mode, filter, dataOn);
    }

    // -------------------------
    // Internal CI-V plumbing
    // (assume: port is open and SyncRoot is held)
    // -------------------------

    private void SendFrame_NoScope(ReadOnlySpan<byte> payload)
    {
        // FE FE <to> <from> <payload...> FD
        Span<byte> frame = stackalloc byte[2 + 2 + payload.Length + 1];
        frame[0] = 0xFE;
        frame[1] = 0xFE;
        frame[2] = radioAddress;
        frame[3] = controllerAddress;

        payload.CopyTo(frame.Slice(4));
        frame[^1] = 0xFD;

        _port.Write(frame.ToArray(), 0, frame.Length);
    }

    private ReadOnlyMemory<byte> QueryFrameExpecting_NoScope(
        ReadOnlySpan<byte> requestPayload,
        Func<ReadOnlySpan<byte>, bool> predicate,
        int overallTimeoutMs = 900)
    {
        SendFrame_NoScope(requestPayload);

        var start = Environment.TickCount;

        while (Environment.TickCount - start < overallTimeoutMs)
        {
            ReadOnlyMemory<byte> frame;
            try
            {
                frame = ReadFrame_NoScope(timeoutMs: 300);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CI-V] ReadFrame error: {ex}");
                continue;
            }

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

        // Expect replies from radio -> controller
        if (from != radioAddress || to != controllerAddress)
            return false;

        payload = frame[4..^1];
        return true;
    }

    private ReadOnlyMemory<byte> ReadFrame_NoScope(int timeoutMs)
    {
        var start = Environment.TickCount;
        var buf = new List<byte>(64);

        while (Environment.TickCount - start < timeoutMs)
        {
            if (_port.BytesToRead == 0)
            {
                Thread.Sleep(5);
                continue;
            }

            var b = _port.ReadByte();
            if (b < 0) continue;

            var by = (byte)b;

            switch (buf.Count)
            {
                case 0:
                    if (by == 0xFE) buf.Add(by);
                    continue;

                case 1:
                    if (by == 0xFE) buf.Add(by);
                    else buf.Clear();
                    continue;
            }

            buf.Add(by);

            if (by == 0xFD && buf.Count >= 6)
                return buf.ToArray();

            if (buf.Count > 256)
                buf.Clear();
        }

        throw new TimeoutException("Timed out waiting for a CI-V frame.");
    }

    // -------------------------
    // Frequency BCD helpers
    // -------------------------

    private static byte[] EncodeFrequencyBcd5(long frequencyHz)
    {
        var digits = frequencyHz.ToString();
        if (digits.Length > 10)
            digits = digits[^10..];

        digits = digits.PadLeft(10, '0');

        var bcd = new byte[5];
        for (var i = 0; i < 5; i++)
        {
            var pairStart = digits.Length - 2 * (i + 1);
            var hi = digits[pairStart] - '0';
            var lo = digits[pairStart + 1] - '0';
            bcd[i] = (byte)((hi << 4) | lo);
        }

        return bcd;
    }

    private static long DecodeFrequencyBcd5(ReadOnlySpan<byte> bcd5)
    {
        if (bcd5.Length != 5)
            throw new ArgumentException("Expected exactly 5 BCD bytes.", nameof(bcd5));

        Span<char> digits = stackalloc char[10];
        var idx = 0;

        for (var i = 4; i >= 0; i--)
        {
            var b = bcd5[i];
            var hi = (b >> 4) & 0xF;
            var lo = b & 0xF;

            if (hi > 9 || lo > 9)
                throw new FormatException($"Invalid BCD digit in byte 0x{b:X2}.");

            digits[idx++] = (char)('0' + hi);
            digits[idx++] = (char)('0' + lo);
        }

        return long.Parse(digits);
    }

    public void Dispose()
    {
        lock (SyncRoot)
        {
            try
            {
                if (_port.IsOpen)
                    _port.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[CI-V] Dispose close error: {ex}");
            }

            _port.Dispose();
        }
    }
}
