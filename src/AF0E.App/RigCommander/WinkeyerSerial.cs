using System.Diagnostics.CodeAnalysis;
using System.IO.Ports;
using System.Text;
using RigCommander.Abstractions;
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
    private int _statusVersion;

    // Cached speed state
    private int? _speedPotRaw;
    private int? _speedPotWpm;
    private int? _hostWpm; // null = follow pot

    private readonly int _minWpm;
    private readonly int _maxWpm;

    private readonly IScriptActivityLog? _activityLog;

    // Set by Abort() without acquiring the lock so it can interrupt a running repeat loop
    private volatile bool _abortRequested;

    // Incremented for every SendScript call; older cycles stop when superseded by a newer token.
    private int _sendCycleToken;

    public WinkeyerSerial(string portName, int baudRate, int minWpm, int maxWpm, TimeSpan idleTimeout, ILogger logger, IScriptActivityLog? activityLog = null)
    {
        if (minWpm < 5)
            throw new ArgumentOutOfRangeException(nameof(minWpm), "MinWpm must be at least 5");
        if (maxWpm <= minWpm)
            throw new ArgumentOutOfRangeException(nameof(maxWpm), "MaxWpm must be greater than MinWpm");

        _minWpm = minWpm;
        _maxWpm = maxWpm;
        _idleTimeout = idleTimeout;
        _logger = logger;
        _activityLog = activityLog;

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

        var cycleToken = Interlocked.Increment(ref _sendCycleToken);

        // Preempt any in-flight cycle immediately by clearing keyer buffer.
        lock (_lock)
        {
            if (_port.IsOpen && _hostOpen)
            {
                WriteBytes_NoLock(0x0A); // Clear Buffer
                Touch_NoLock();
            }
        }

        _abortRequested = false;
        var totalSent = 0;
        var retried = false;

        totalSent += ExecuteRepeatLoop(script, bytes, repeat, repeatDelaySeconds, cycleToken, ref retried);

        var cancelled = IsCycleCancelled(cycleToken, CancellationToken.None);

        _logger.LogDebug("[Winkeyer] Script summary: requested={Requested}, sent={Sent}, delay={Delay}s, cancelled={Cancelled}, retried={Retried}",
            repeat, totalSent, repeatDelaySeconds, cancelled, retried);

        // Log final summary only if there's additional context beyond per-send logs
        if (repeat <= 1 && !cancelled && !retried)
            return;

        var entry = BuildActivityEntry(script, repeat, repeatDelaySeconds, totalSent, cancelled, retried);
        if (cancelled || retried)
            _activityLog?.LogWarning(entry);
        else
            _activityLog?.LogInformation(entry);
    }

    private int ExecuteRepeatLoop(string script, byte[] bytes, int repeat, int repeatDelaySeconds, int cycleToken, ref bool retried)
    {
        var sent = 0;

        for (var i = 0; i < repeat; i++)
        {
            if (IsCycleCancelled(cycleToken, CancellationToken.None))
                break;

            var waitStatusVersion = SendScriptBytesWithRetry(bytes, out var sendRetried);
            retried |= sendRetried;
            sent++;

            _logger.LogDebug("[Winkeyer] Sent script iteration {Iteration}/{Repeat}", sent, repeat);

            // Log per-send activity immediately with full script text
            _activityLog?.LogInformation(repeat > 1 ? $"{script}  [{sent}/{repeat}]" : script);

            if (i >= repeat - 1)
                continue;

            if (!WaitUntilKeyerIsFree(cycleToken, waitStatusVersion, CancellationToken.None))
                break;

            _logger.LogDebug("[Winkeyer] Iteration {Iteration}/{Repeat} completed. Starting repeat delay of {DelaySeconds}s.",
                sent, repeat, repeatDelaySeconds);

            if (!WaitForRepeatDelay(cycleToken, repeatDelaySeconds, CancellationToken.None))
                break;
        }

        return sent;
    }

    private static string BuildActivityEntry(string script, int requestedRepeat, int repeatDelaySeconds, int sent, bool aborted, bool retried)
    {
        if (requestedRepeat <= 1)
        {
            var suffixParts = new List<string>();
            if (aborted)
                suffixParts.Add("aborted");
            if (retried)
                suffixParts.Add("retried");

            return suffixParts.Count == 0
                ? script
                : $"{script}  ({string.Join(", ", suffixParts)})";
        }

        var details = new List<string>
        {
            $"sent {sent}/{requestedRepeat}"
        };

        if (repeatDelaySeconds > 0)
            details.Add($"{repeatDelaySeconds}s delay");

        if (aborted)
            details.Add("aborted");

        if (retried)
            details.Add("retried");

        return $"{script}  ({string.Join(", ", details)})";
    }

    private int SendScriptBytesWithRetry(byte[] bytes, out bool retried)
    {
        try
        {
            retried = false;
            return SendScriptBytes(bytes);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Winkeyer] Script send failed; reconnecting");

            lock (_lock)
            {
                ForceDisconnect_NoLock();
            }

            retried = true;
            return SendScriptBytes(bytes);
        }
    }

    private int SendScriptBytes(byte[] bytes)
    {
        lock (_lock)
        {
            Touch_NoLock();
            EnsureConnected_NoLock();

            // Drop any pending status from the previous send cycle so the repeat loop
            // waits for a fresh busy/free update generated after this write.
            _port.DiscardInBuffer();

            if (!_hostOpen)
                throw new InvalidOperationException("Winkeyer host is not open.");

            _port.Write(bytes, 0, bytes.Length);
            _port.BaseStream.Flush();

            // Prevent immediate re-send on stale cached status; wait loop will clear this
            // once the keyer reports it is no longer busy.
            _busy = true;

            Touch_NoLock();
            _logger.LogDebug("[Winkeyer] Script bytes queued. Waiting for fresh status after version {StatusVersion}.", _statusVersion);
            return _statusVersion;
        }
    }

    private bool WaitUntilKeyerIsFree(int cycleToken, int minimumStatusVersion, CancellationToken ct)
    {
        var mustSeeBusyUntilUtc = DateTime.UtcNow.AddMilliseconds(250);
        var waitStartedUtc = DateTime.UtcNow;
        var sawActiveStateSinceSend = false;
        var loggedGuardWindow = false;

        while (true)
        {
            if (IsCycleCancelled(cycleToken, ct))
                return false;

            bool hasFreshStatus;
            bool isKnown;
            bool isFree;

            lock (_lock)
            {
                Touch_NoLock();
                EnsureConnected_NoLock();
                WriteBytes_NoLock(0x15); // Get Status
                hasFreshStatus = _statusVersion > minimumStatusVersion;
                isKnown = _busy.HasValue || _wait.HasValue || _xoff.HasValue;
                isFree = !(_busy ?? false) && !(_wait ?? false) && !(_xoff ?? false);

                if (hasFreshStatus && isKnown && !isFree)
                {
                    if (!sawActiveStateSinceSend)
                    {
                        _logger.LogDebug(
                            "[Winkeyer] First active status after send observed at {ElapsedMs} ms (busy={Busy}, wait={Wait}, xoff={Xoff}, version={StatusVersion}).",
                            (DateTime.UtcNow - waitStartedUtc).TotalMilliseconds,
                            _busy,
                            _wait,
                            _xoff,
                            _statusVersion);
                    }

                    sawActiveStateSinceSend = true;
                }
            }

            if (hasFreshStatus && isKnown && isFree)
            {
                if (sawActiveStateSinceSend)
                {
                    _logger.LogDebug(
                        "[Winkeyer] Fresh free status accepted after active state at {ElapsedMs} ms (version={StatusVersion}).",
                        (DateTime.UtcNow - waitStartedUtc).TotalMilliseconds,
                        _statusVersion);
                    return true;
                }

                if (DateTime.UtcNow >= mustSeeBusyUntilUtc)
                {
                    _logger.LogDebug(
                        "[Winkeyer] Fresh free status accepted after guard window at {ElapsedMs} ms without observing active state first (version={StatusVersion}).",
                        (DateTime.UtcNow - waitStartedUtc).TotalMilliseconds,
                        _statusVersion);
                    return true;
                }

                if (!loggedGuardWindow)
                {
                    _logger.LogDebug(
                        "[Winkeyer] Fresh free status arrived before active state; holding until guard window expires");
                    loggedGuardWindow = true;
                }
            }

            Thread.Sleep(100);
        }
    }

    private bool WaitForRepeatDelay(int cycleToken, int repeatDelaySeconds, CancellationToken ct)
    {
        if (repeatDelaySeconds <= 0)
            return !IsCycleCancelled(cycleToken, ct);

        var delayStartedUtc = DateTime.UtcNow;
        var remaining = TimeSpan.FromSeconds(repeatDelaySeconds);

        while (remaining > TimeSpan.Zero)
        {
            if (IsCycleCancelled(cycleToken, ct))
                return false;

            var slice = remaining > TimeSpan.FromMilliseconds(100)
                ? TimeSpan.FromMilliseconds(100)
                : remaining;

            Thread.Sleep(slice);
            remaining -= slice;
        }

        _logger.LogDebug("[Winkeyer] Repeat delay completed in {ElapsedMs} ms.", (DateTime.UtcNow - delayStartedUtc).TotalMilliseconds);
        return !IsCycleCancelled(cycleToken, ct);
    }

    private bool IsCycleCancelled(int cycleToken, CancellationToken ct)
        => _abortRequested || ct.IsCancellationRequested || Volatile.Read(ref _sendCycleToken) != cycleToken;

    public void Abort()
    {
        // Signal any running repeat loop to stop immediately, without needing the lock
        _abortRequested = true;

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
                _statusVersion++;
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
        return [.. output];

        void FlushText()
        {
            if (textBuffer.Length == 0)
                return;

            var sanitized = SanitizePlainText(textBuffer.ToString());
            if (!string.IsNullOrEmpty(sanitized))
                output.AddRange(Encoding.ASCII.GetBytes(sanitized));

            textBuffer.Clear();
        }
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
            // the parser already handles escaped //
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
