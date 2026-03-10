using System.Diagnostics.CodeAnalysis;
using System.IO.Ports;
using System.Text;
using Timer = System.Threading.Timer;

namespace RigCommander;

public sealed record WinkeyerStatus(
    bool PortOpen,
    bool HostOpen,
    int? Revision,
    DateTime? LastActivityUtc,
    double? IdleSeconds,
    bool? Busy,
    bool? Wait,
    bool? Xoff,
    int? SpeedPotRaw,
    int? SpeedPotWpm,
    int? HostWpm,
    int? EffectiveWpm,
    int MinWpm,
    int MaxWpm,
    int WpmRange
);

[SuppressMessage("Performance", "CA1848:Use the LoggerMessage delegates")]
[SuppressMessage("Performance", "CA1873:Avoid potentially expensive logging")]
[SuppressMessage("Style", "IDE0230:Use UTF-8 string literal")]
public sealed class WinkeyerSerial : IDisposable
{
    private readonly SerialPort _port;
    private readonly Lock _lock = new();

    private readonly TimeSpan _idleTimeout;
    private readonly ILogger _logger;
    private readonly Timer _idleTimer;

    private CancellationTokenSource? _readerCts;
    private Task? _readerTask;

    private bool _hostOpen;
    private int? _revision;
    private DateTime _lastActivityUtc = DateTime.MinValue;

    // Cached unsolicited status
    private bool? _busy;
    private bool? _wait;
    private bool? _xoff;

    // Cached speed state
    private int? _speedPotRaw;
    private int? _speedPotWpm;
    private int? _hostWpm; // null = follow pot

    private readonly int _minWpm;
    private readonly int _maxWpm;

    public WinkeyerSerial(string portName, int baudRate, int minWpm, int maxWpm, TimeSpan idleTimeout, ILogger logger)
    {
        if (minWpm < 5)
            throw new ArgumentOutOfRangeException(nameof(minWpm), "MinWpm must be at least 5");
        if (maxWpm <= minWpm)
            throw new ArgumentOutOfRangeException(nameof(maxWpm), "MaxWpm must be greater than MinWpm");

        _minWpm = minWpm;
        _maxWpm = maxWpm;
        _idleTimeout = idleTimeout;
        _logger = logger;

        _port = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.Two)
        {
            Handshake = Handshake.None,
            ReadTimeout = 800,
            WriteTimeout = 800,
            DtrEnable = true,
            RtsEnable = false
        };

        _idleTimer = new Timer(
            _ => CloseIfIdle(),
            null,
            dueTime: TimeSpan.FromSeconds(5),
            period: TimeSpan.FromSeconds(5));
    }

    public void EnsureReady()
    {
        lock (_lock)
        {
            Touch_NoLock();
            EnsureConnected_NoLock();
        }
    }

    public void SetWpm(int wpm)
    {
        lock (_lock)
        {
            EnsureConnected_NoLock();

            // 0 means "follow speed pot"
            if (wpm != 0 && (wpm < _minWpm || wpm > _maxWpm))
                throw new ArgumentOutOfRangeException(nameof(wpm), $"WPM must be 0 or between {_minWpm} and {_maxWpm}.");

            WriteBytes_NoLock(0x02, (byte)wpm);

            _hostWpm = wpm == 0 ? null : wpm;
            Touch_NoLock();

            // Ask for fresh async updates
            WriteBytes_NoLock(0x07); // Get Pot
            WriteBytes_NoLock(0x15); // Get Status
        }
    }

    public void SendScript(string script, int repeat = 1, int repeatDelaySeconds = 0)
    {
        if (string.IsNullOrWhiteSpace(script))
            return;

        if (repeat < 1)
            throw new ArgumentOutOfRangeException(nameof(repeat), "Repeat must be at least 1.");

        if (repeatDelaySeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(repeatDelaySeconds), "RepeatDelaySeconds must be at least 0.");

        var bytes = BuildScriptBytes(script);
        if (bytes.Length == 0)
            return;

        lock (_lock)
        {
            Touch_NoLock();
            EnsureConnected_NoLock();

            if (!_hostOpen)
                throw new InvalidOperationException("Winkeyer host is not open.");

            try
            {
                for (var i = 0; i < repeat; i++)
                {
                    _port.Write(bytes, 0, bytes.Length);

                    if (i < repeat - 1 && repeatDelaySeconds > 0)
                    {
                        // WK3 host-mode buffered wait command
                        // 0x1A <seconds>
                        WriteBytes_NoLock(0x1A, (byte)Math.Min(repeatDelaySeconds, 255));
                    }
                }

                _port.BaseStream.Flush();

                Touch_NoLock();
                _logger.LogDebug("[Winkeyer] Sent script \"{Script}\" x{Repeat} with {Delay}s delay ({Length} bytes each)", script, repeat, repeatDelaySeconds, bytes.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Winkeyer] Script send failed; reconnecting");

                ForceDisconnect_NoLock();

                Touch_NoLock();
                EnsureConnected_NoLock();

                for (var i = 0; i < repeat; i++)
                {
                    _port.Write(bytes, 0, bytes.Length);

                    if (i < repeat - 1 && repeatDelaySeconds > 0)
                    {
                        WriteBytes_NoLock(0x1A, (byte)Math.Min(repeatDelaySeconds, 255));
                    }
                }

                _port.BaseStream.Flush();

                Touch_NoLock();
            }
        }
    }

    public void Abort()
    {
        lock (_lock)
        {
            Touch_NoLock();
            EnsureConnected_NoLock();

            // Clear Buffer
            WriteBytes_NoLock(0x0A);
        }
    }

    public WinkeyerStatus GetStatus()
    {
        lock (_lock)
        {
            double? idle = null;

            if (_lastActivityUtc != DateTime.MinValue)
                idle = (DateTime.UtcNow - _lastActivityUtc).TotalSeconds;

            return new WinkeyerStatus(
                PortOpen: _port.IsOpen,
                HostOpen: _hostOpen,
                Revision: _revision,
                LastActivityUtc: _lastActivityUtc == DateTime.MinValue ? null : _lastActivityUtc,
                IdleSeconds: idle,
                Busy: _busy,
                Wait: _wait,
                Xoff: _xoff,
                SpeedPotRaw: _speedPotRaw,
                SpeedPotWpm: _speedPotWpm,
                HostWpm: _hostWpm,
                EffectiveWpm: GetEffectiveWpm_NoLock(),
                MinWpm: _minWpm,
                MaxWpm: _maxWpm,
                WpmRange: _maxWpm - _minWpm
            );
        }
    }

    private int? GetEffectiveWpm_NoLock() => _hostWpm ?? _speedPotWpm;

    private void Touch_NoLock()
    {
        _lastActivityUtc = DateTime.UtcNow;
    }

    private void EnsureConnected_NoLock()
    {
        switch (_port.IsOpen)
        {
            case true when _hostOpen:
                return;

            case false:
                _port.Open();

                // WinKey USB devices often need a short settle time after open
                Thread.Sleep(250);

                _port.DiscardInBuffer();
                _port.DiscardOutBuffer();
                break;
        }

        if (_hostOpen)
            return;

        // Host Open: 00 02 -> revision
        WriteBytes_NoLock(0x00, 0x02);
        _revision = ReadByteWithTimeout_NoLock(1200);
        _hostOpen = true;

        _logger.LogDebug("[Winkeyer] Host opened. Revision={Revision}", _revision);

        // Setup Speed Pot: 0x05 <MIN> <RANGE> <compat>
        // For WK2/WK3 the last byte is 0.
        WriteBytes_NoLock(0x05, (byte)_minWpm, (byte)(_maxWpm - _minWpm), 0x00);

        StartReader_NoLock();

        // Pot mode on initial connect
        WriteBytes_NoLock(0x02, 0x00);
        _hostWpm = null;

        // Prime initial async state
        WriteBytes_NoLock(0x07); // Get Pot
        WriteBytes_NoLock(0x15); // Get Status
    }

    private void StartReader_NoLock()
    {
        if (_readerTask is { IsCompleted: false })
            return;

        _readerCts = new CancellationTokenSource();
        var token = _readerCts.Token;

        _readerTask = Task.Run(() => ReaderLoop(token), token);
    }

    private void StopReader_NoLock()
    {
        try
        {
            _readerCts?.Cancel();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "[Winkeyer] Reader cancellation threw");
        }

        _readerCts?.Dispose();
        _readerCts = null;
        _readerTask = null;
    }

    private void ReaderLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                int? nextByte = null;

                lock (_lock)
                {
                    if (!_port.IsOpen || !_hostOpen)
                    {
                        // no-op, sleep outside lock
                    }
                    else if (_port.BytesToRead > 0)
                    {
                        var b = _port.ReadByte();
                        if (b >= 0)
                            nextByte = b;
                    }
                }

                if (nextByte is null)
                {
                    Thread.Sleep(20);
                    continue;
                }

                lock (_lock)
                {
                    ProcessIncomingByte_NoLock((byte)nextByte.Value);
                }
            }
            catch (Exception e) when (e is OperationCanceledException or ObjectDisposedException)
            {
                break;
            }
            catch (InvalidOperationException)
            {
                // Port likely closed between checks
                Thread.Sleep(50);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Winkeyer] Reader loop failed");
                Thread.Sleep(50);
            }
        }
    }

    private void ProcessIncomingByte_NoLock(byte b)
    {
        switch (b & 0b1100_0000)
        {
            // Unsolicited status byte: 11xxxxxx
            case 0b1100_0000:
                _wait = (b & (1 << 4)) != 0;
                _busy = (b & (1 << 2)) != 0;
                _xoff = (b & (1 << 0)) != 0;
                break;
            // Unsolicited speed pot byte: 10xxxxxx
            case 0b1000_0000:
            {
                var raw = b & 0b0011_1111;
                _speedPotRaw = raw;
                _speedPotWpm = ComputeSpeedWpm(raw);
                break;
            }
        }

        // Other bytes are likely echoes or incidental responses; ignore.
    }

    private int ComputeSpeedWpm(int raw)
    {
        var clamped = Math.Clamp(raw, 0, _maxWpm - _minWpm);
        return _minWpm + clamped;
    }

    private void CloseIfIdle()
    {
        lock (_lock)
        {
            if (!_port.IsOpen && !_hostOpen)
                return;

            var idleFor = DateTime.UtcNow - _lastActivityUtc;
            if (idleFor < _idleTimeout)
                return;

            // Use cached async status. If still active/backpressured, do not close.
            if ((_busy ?? false) || (_wait ?? false) || (_xoff ?? false))
            {
                _logger.LogDebug(
                    "[Winkeyer] Idle but busy/wait/xoff (busy={Busy}, wait={Wait}, xoff={Xoff}). Not closing.",
                    _busy, _wait, _xoff);
                return;
            }

            _logger.LogDebug(
                "[Winkeyer] Idle for {IdleSeconds:F0}s and not busy. Closing host/port.",
                idleFor.TotalSeconds);

            ForceDisconnect_NoLock();
        }
    }

    private void ForceDisconnect_NoLock()
    {
        try
        {
            StopReader_NoLock();

            if (!_port.IsOpen) return;

            if (_hostOpen)
            {
                try
                {
                    // Clear Buffer, then Host Close
                    WriteBytes_NoLock(0x0A);
                    WriteBytes_NoLock(0x00, 0x03);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[Winkeyer] HostClose failed");
                }
            }

            try
            {
                _port.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Winkeyer] Port close failed");
            }
        }
        finally
        {
            _hostOpen = false;
            _revision = null;

            _busy = null;
            _wait = null;
            _xoff = null;

            _speedPotRaw = null;
            _speedPotWpm = null;
            _hostWpm = null;
        }
    }

    private void WriteBytes_NoLock(params byte[] bytes)
    {
        _port.Write(bytes, 0, bytes.Length);
    }

    private int ReadByteWithTimeout_NoLock(int timeoutMs)
    {
        var start = Environment.TickCount;

        while (Environment.TickCount - start < timeoutMs)
        {
            if (_port.BytesToRead > 0)
                return _port.ReadByte();

            Thread.Sleep(5);
        }

        throw new TimeoutException("Timed out waiting for byte from Winkeyer.");
    }

    private byte[] BuildScriptBytes(string script)
    {
        var output = new List<byte>();
        var textBuffer = new StringBuilder();

        void FlushText()
        {
            if (textBuffer.Length == 0)
                return;

            var sanitized = SanitizePlainText(textBuffer.ToString());
            if (!string.IsNullOrEmpty(sanitized))
                output.AddRange(Encoding.ASCII.GetBytes(sanitized));

            textBuffer.Clear();
        }

        for (var i = 0; i < script.Length; i++)
        {
            var ch = script[i];

            if (ch != '/')
            {
                textBuffer.Append(ch);
                continue;
            }

            if (i + 1 >= script.Length)
            {
                textBuffer.Append('/');
                break;
            }

            var cmd = char.ToUpperInvariant(script[i + 1]);

            // literal slash
            if (cmd == '/')
            {
                textBuffer.Append('/');
                i++;
                continue;
            }

            FlushText();

            switch (cmd)
            {
                case 'R':
                {
                    // /Rxy -> merge x+y into a prosign
                    if (i + 3 >= script.Length)
                        throw new ArgumentException("/R requires two following characters, e.g. /RAR or /RBT.");

                    var c1 = ToPrintableAscii(script[i + 2]);
                    var c2 = ToPrintableAscii(script[i + 3]);

                    output.Add(0x1B);          // MERGE
                    output.Add((byte)c1);
                    output.Add((byte)c2);

                    i += 3;
                    break;
                }

                case 'S':
                {
                    // /S20 -> buffered speed change
                    var nn = ReadTwoDigits(script, i + 2, "/S requires two digits, e.g. /S20.");
                    if (nn < _minWpm || nn > _maxWpm)
                        throw new ArgumentException($"/S speed must be between {_minWpm} and {_maxWpm}.");

                    output.Add(0x1C);          // CHANGE_BFR_SPD
                    output.Add((byte)nn);

                    i += 3;
                    break;
                }

                case 'W':
                {
                    // /W05 -> wait 5 seconds
                    var nn = ReadTwoDigits(script, i + 2, "/W requires two digits, e.g. /W05.");

                    output.Add(0x1A);          // WAIT
                    output.Add((byte)nn);

                    i += 3;
                    break;
                }

                case 'K':
                {
                    // /K03 -> key down 3 seconds
                    var nn = ReadTwoDigits(script, i + 2, "/K requires two digits, e.g. /K03.");

                    output.Add(0x19);          // KEY_BUFFERED
                    output.Add((byte)nn);

                    i += 3;
                    break;
                }

                case 'X':
                {
                    // /X -> cancel buffered speed change
                    output.Add(0x1E);          // CANCEL_BFR_SPD
                    i += 1;
                    break;
                }

                default:
                    throw new ArgumentException($"Unknown Winkeyer script command '/{cmd}'.");
            }
        }

        FlushText();
        return output.ToArray();
    }

    private static int ReadTwoDigits(string text, int start, string errorMessage)
    {
        if (start + 1 >= text.Length ||
            !char.IsDigit(text[start]) ||
            !char.IsDigit(text[start + 1]))
        {
            throw new ArgumentException(errorMessage);
        }

        return (text[start] - '0') * 10 + (text[start + 1] - '0');
    }

    private static char ToPrintableAscii(char ch)
    {
        ch = char.ToUpperInvariant(ch);

        if (ch is '\r' or '\n' or '\t')
            return ' ';

        if (ch < 0x20 || ch > 0x7E)
            throw new ArgumentException("Embedded Winkeyer commands require printable ASCII characters.");

        return ch;
    }

    private static string SanitizePlainText(string text)
    {
        text = text.ToUpperInvariant();

        var sb = new StringBuilder(text.Length);

        foreach (var ch in text)
        {
            if (ch is '\r' or '\n' or '\t')
            {
                sb.Append(' ');
                continue;
            }

            if (ch < 0x20)
                continue;

            // plain text should not contain slash commands at this point;
            // escaped // is already handled by the parser
            sb.Append(ch <= 0x7E ? ch : '?');
        }

        return sb.ToString();
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _idleTimer.Dispose();
            ForceDisconnect_NoLock();
            _port.Dispose();
        }
    }
}
