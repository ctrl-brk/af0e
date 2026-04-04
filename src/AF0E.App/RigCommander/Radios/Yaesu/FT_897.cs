using System.IO.Ports;
using RigCommander.Abstractions;
using RigCommander.Contracts;

namespace RigCommander.Radios.Yaesu;

// ReSharper disable once InconsistentNaming
public sealed class FT_897 : IRadio
{
    private readonly SerialPort _port;
    private readonly Lock _lock = new();
    private readonly bool? _dtrEnable;
    private readonly bool? _rtsEnable;
    private readonly int _replyDelayMs;
    private readonly int _readTimeoutMs;

    public FT_897(string portName, int baudRate, bool? dtrEnable = null, bool? rtsEnable = null, int replyDelayMs = 40, int readTimeoutMs = 2000)
    {
        _dtrEnable = dtrEnable;
        _rtsEnable = rtsEnable;
        _replyDelayMs = replyDelayMs;
        _readTimeoutMs = readTimeoutMs;

        _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.Two)
        {
            Handshake = Handshake.None,
            ReadTimeout = 600,
            WriteTimeout = 600
        };

        if (_dtrEnable.HasValue)
            _port.DtrEnable = _dtrEnable.Value;

        if (_rtsEnable.HasValue)
            _port.RtsEnable = _rtsEnable.Value;
    }

    public T WithConnection<T>(Func<T> action)
    {
        lock (_lock)
        {
            try
            {
                if (!_port.IsOpen)
                {
                    _port.Open();

                    if (_dtrEnable.HasValue)
                        _port.DtrEnable = _dtrEnable.Value;

                    if (_rtsEnable.HasValue)
                        _port.RtsEnable = _rtsEnable.Value;
                }

                _port.DiscardInBuffer();
                return action();
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
                    Console.WriteLine($"[FT-897] Error closing port: {ex}");
                }
            }
        }
    }

    public long GetFrequency() => GetStatus().FrequencyHz;

    public void SetFrequency(long frequencyHz)
    {
        var v = frequencyHz / 10;
        var digits = v.ToString().PadLeft(8, '0');
        var p = PackBcd8(digits);

        Send5_NoScope(p[0], p[1], p[2], p[3], 0x01);
    }

    /// <summary>
    /// Sets FT-8x7 mode
    /// </summary>
    /// <param name="modeText">Mode</param>
    /// <param name="filter">Not supported by FT-8x7</param>
    public void SetMode(string modeText, byte filter)
    {
        var (modeCode, _) = ParseMode(modeText);
        Send5_NoScope(modeCode, 0x00, 0x00, 0x00, 0x07);
    }

    /// <summary>
    /// Noop. Not supported by FT-8x7
    /// </summary>
    public void SetNoiseReduction(bool enabled) {}

    /// <summary>
    /// Noop. Not supported by FT-8x7
    /// </summary>
    public void SetNoiseBlanker(bool enabled) {}

    public RadioStatus GetStatus()
    {
        Send5_NoScope(0x00, 0x00, 0x00, 0x00, 0x03);
        Thread.Sleep(_replyDelayMs);
        var reply = ReadExact_NoScope(5, timeoutMs: _readTimeoutMs);

        var digits = UnpackBcd8(reply[0], reply[1], reply[2], reply[3]);
        var hz = long.Parse(digits) * 10;

        var modeCode = reply[4];
        var mode = ModeCodeToText(modeCode);
        var dataOn = mode is "DIG" or "PKT";

        return new RadioStatus(hz, mode, Filter: null, DataModeOn: dataOn, NoiseReductionOn: false, NoiseBlankerOn: false, SplitOn: GetSplit());
    }

    private bool GetSplit()
    {
        // Read RX Status
        Send5_NoScope(0x00, 0x00, 0x00, 0x00, 0xE7);
        Thread.Sleep(_replyDelayMs);

        var reply = ReadExact_NoScope(1, timeoutMs: _readTimeoutMs);

        // Bit 5 = split status
        // 0 => split ON
        // 1 => split OFF
        var splitBit = (reply[0] >> 5) & 0x01;

        return splitBit == 0;
    }

    // ReSharper disable once UnusedTupleComponentInReturnValue
    private static (byte modeCode, bool dataOn) ParseMode(string modeText)
    {
        if (string.IsNullOrWhiteSpace(modeText))
            throw new ArgumentException("Mode text is required.", nameof(modeText));

        var token = modeText
            .Trim()
            .Replace("-", string.Empty)
            .Replace("_", string.Empty)
            .Replace(" ", string.Empty)
            .ToUpperInvariant();

        return token switch
        {
            "LSB" => (0x00, false),
            "USB" => (0x01, false),
            "CW" => (0x02, false),
            "CWR" => (0x03, false),
            "AM" => (0x04, false),
            "WFM" => (0x06, false),
            "FM" => (0x08, false),
            "FMN" => (0x88, false),
            "PKT" or "PACKET" => (0x0C, true),
            "DIG" or "DIGU" or "DIGL" or "FT8" or "FT4" or "FT2" or "USBD" or "LSBD" => (0x0A, true),
            _ => throw new ArgumentException("Unsupported Mode for FT-897. Try: LSB, USB, CW, CWR, AM, FM, FMN, WFM, DIG (FT8/FT4/FT2), PKT.")
        };
    }

    private static string ModeCodeToText(byte code) => code switch
    {
        0x00 => "LSB",
        0x01 => "USB",
        0x02 => "CW",
        0x03 => "CWR",
        0x04 => "AM",
        0x06 => "WFM",
        0x08 => "FM",
        0x0A => "DIG",
        0x0C => "PKT",
        0x88 => "FMN",
        _ => $"0x{code:X2}"
    };

    private void Send5_NoScope(byte b1, byte b2, byte b3, byte b4, byte opcode)
    {
        Span<byte> buf = [b1, b2, b3, b4, opcode];
        _port.Write(buf.ToArray(), 0, 5);
    }

    private byte[] ReadExact_NoScope(int count, int timeoutMs)
    {
        var data = new byte[count];
        var offset = 0;
        var deadline = Environment.TickCount64 + timeoutMs;

        while (offset < count)
        {
            var remaining = (int)Math.Max(1, deadline - Environment.TickCount64);
            if (remaining <= 0)
                throw new TimeoutException($"Timed out waiting for {count} CAT bytes (got {offset}).");

            var previousTimeout = _port.ReadTimeout;
            try
            {
                _port.ReadTimeout = remaining;
                var n = _port.Read(data, offset, count - offset);
                if (n > 0)
                {
                    offset += n;
                    deadline = Environment.TickCount64 + timeoutMs;
                }
            }
            catch (TimeoutException)
            {
                throw new TimeoutException($"Timed out waiting for {count} CAT bytes (got {offset}).");
            }
            finally
            {
                _port.ReadTimeout = previousTimeout;
            }
        }

        return data;
    }

    private static byte[] PackBcd8(string digits8)
    {
        if (digits8.Length != 8) throw new ArgumentException("Expected 8 digits.", nameof(digits8));
        var b = new byte[4];
        for (var i = 0; i < 4; i++)
        {
            var hi = digits8[i * 2] - '0';
            var lo = digits8[i * 2 + 1] - '0';
            b[i] = (byte)((hi << 4) | lo);
        }
        return b;
    }

    private static string UnpackBcd8(byte b1, byte b2, byte b3, byte b4)
    {
        Span<char> d = stackalloc char[8];
        var bytes = new[] { b1, b2, b3, b4 };
        var idx = 0;
        foreach (var b in bytes)
        {
            var hi = (b >> 4) & 0xF;
            var lo = b & 0xF;
            d[idx++] = DecodeBcdNibble((byte)hi);
            d[idx++] = DecodeBcdNibble((byte)lo);
        }
        return new string(d);
    }

    private static char DecodeBcdNibble(byte nibble)
    {
        return nibble switch
        {
            <= 9 => (char)('0' + nibble),
            0xF => '0',
            _ => throw new FormatException($"Invalid BCD nibble 0x{nibble:X}")
        };
    }

    public void Dispose()
    {
        lock (_lock)
        {
            try
            {
                if (_port.IsOpen)
                    _port.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FT-897] Dispose close error: {ex}");
            }
            _port.Dispose();
        }
    }
}
