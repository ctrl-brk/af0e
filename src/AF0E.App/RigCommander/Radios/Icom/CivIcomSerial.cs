using System.IO.Ports;

namespace RigCommander.Radios.Icom;

public sealed class CivIcomSerial(string portName, int baudRate, byte radioAddress, byte controllerAddress) : IDisposable
{
    private readonly SerialPort _port = new(portName, baudRate, Parity.None, 8, StopBits.One)
    {
        Handshake = Handshake.None,
        ReadTimeout = 600,
        WriteTimeout = 600
    };
    private readonly Lock _lock = new();

    public T WithConnection<T>(Func<T> action)
    {
        lock (_lock)
        {
            try
            {
                if (!_port.IsOpen)
                    _port.Open();

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
                catch (Exception)
                {
                    //Console.WriteLine($"[IC-9100] Error closing port: {ex}");
                }
            }
        }
    }

    public long GetFrequency()
    {
        var payload = QueryFrameExpecting(
            requestPayload: [0x03],
            predicate: p => p.Length >= 6 && p[0] == 0x03);

        return DecodeFrequencyBcd5(payload.Span.Slice(1, 5));
    }

    public void SetFrequency(long frequencyHz)
    {
        var freqBcd = EncodeFrequencyBcd5(frequencyHz);

        Span<byte> payload = stackalloc byte[1 + 5];
        payload[0] = 0x05;
        freqBcd.CopyTo(payload.Slice(1, 5));

        SendFrame(payload);
    }

    public (IcomMode mode, byte filter) GetMode()
    {
        var modePayload = QueryFrameExpecting(
            requestPayload: [0x04],
            predicate: p => p.Length >= 3 && p[0] == 0x04);

        return ((IcomMode)modePayload.Span[1], modePayload.Span[2]);
    }

    public void SetMode(IcomMode mode, byte filter = 0x01) =>
        SendFrame([0x06, (byte)mode, filter]);

    public bool GetDataMode()
    {
        var payload = QueryFrameExpecting(
            requestPayload: [0x1A, 0x06],
            predicate: p => p.Length >= 4 && p[0] == 0x1A && p[1] == 0x06);

        return payload.Span[2] != 0x00;
    }

    public void SetDataMode(bool enabled) =>
        SendFrame([0x1A, 0x06, (byte)(enabled ? 0x01 : 0x00), 0x01]);

    public bool GetNoiseReduction()
    {
        var payload = QueryFrameExpecting(
            requestPayload: [0x16, 0x40],
            predicate: p => p.Length >= 3 &&
                            p[0] == 0x16 &&
                            p[1] == 0x40
        );

        return payload.Span[2] != 0x00;
    }

    public void SetNoiseReduction9100(bool enabled)
    {
        // 16 40 00 = NR OFF
        // 16 40 01 = NR ON
        SendFrame([0x16, 0x40, (byte)(enabled ? 0x01 : 0x00)]);
    }

    public void SetNoiseReduction7410(bool enabled)
    {
        SetNoiseReduction9100(enabled);
    }

    public bool GetNoiseBlanker()
    {
        var payload = QueryFrameExpecting(
            requestPayload: [0x16, 0x22],
            predicate: p => p.Length >= 3 &&
                            p[0] == 0x16 &&
                            p[1] == 0x22
        );

        return payload.Span[2] != 0x00;
    }

    public void SetNoiseBlanker(bool enabled)
    {
        // 16 22 00 = NB OFF
        // 16 22 01 = NB ON
        SendFrame([0x16, 0x22, (byte)(enabled ? 0x01 : 0x00)]);
    }

    public IcomRadioStatus GetStatus()
    {
        (IcomMode mode, var filter) = GetMode();
        return new IcomRadioStatus(GetFrequency(), mode, filter, GetDataMode(), GetNoiseReduction(), GetNoiseBlanker(), GetSplit());
    }

    private bool GetSplit()
    {
        var payload = QueryFrameExpecting(
            requestPayload: [0x0F],
            predicate: p => p.Length >= 2 && p[0] == 0x0F
        );

        return payload.Span[1] switch
        {
            0x00 => false,
            0x01 => true,
            0x11 => true, // DUP-
            0x12 => true, // DUP+
            _ => throw new InvalidOperationException($"Unknown split status value 0x{payload.Span[1]:X2}.")
        };
    }

    private void SendFrame(ReadOnlySpan<byte> payload)
    {
        Span<byte> frame = stackalloc byte[2 + 2 + payload.Length + 1];
        frame[0] = 0xFE;
        frame[1] = 0xFE;
        frame[2] = radioAddress;
        frame[3] = controllerAddress;

        payload.CopyTo(frame[4..]);
        frame[^1] = 0xFD;

        _port.Write(frame.ToArray(), 0, frame.Length);
    }

    private ReadOnlyMemory<byte> QueryFrameExpecting(ReadOnlySpan<byte> requestPayload, Func<ReadOnlySpan<byte>, bool> predicate, int overallTimeoutMs = 900)
    {
        SendFrame(requestPayload);

        var start = Environment.TickCount;

        while (Environment.TickCount - start < overallTimeoutMs)
        {
            ReadOnlyMemory<byte> frame;
            try
            {
                frame = ReadFrame(timeoutMs: 300);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IC-9100] ReadFrame error: {ex}");
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

        if (from != radioAddress || to != controllerAddress)
            return false;

        payload = frame[4..^1];
        return true;
    }

    private ReadOnlyMemory<byte> ReadFrame(int timeoutMs)
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
        lock (_lock)
        {
            try
            {
                if (_port.IsOpen)
                    _port.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IC-9100] Dispose close error: {ex}");
            }

            _port.Dispose();
        }
    }
}
