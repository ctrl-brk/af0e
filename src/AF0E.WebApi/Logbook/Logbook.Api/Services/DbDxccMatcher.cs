using System.Text;
using System.Text.RegularExpressions;
using AF0E.Common.Radio;
using AF0E.DB;
using AF0E.Services.DxCluster;
using Microsoft.EntityFrameworkCore;

namespace Logbook.Api.Services;

public sealed partial class DbDxccMatcher(IServiceScopeFactory serviceScopeFactory, ILogger<DbDxccMatcher> logger) : IDxccMatcher, IAsyncDisposable
{
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromHours(6);
    private static readonly TimeSpan StatusCacheLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

    private readonly SemaphoreSlim _reloadGate = new(1, 1);
    private readonly Lock _statusCacheSync = new();
    private readonly Dictionary<StatusCacheKey, StatusCacheEntry> _statusCache = [];
    private Snapshot _snapshot = Snapshot.Empty;
    private DateTimeOffset _loadedAtUtc = DateTimeOffset.MinValue;

    public async ValueTask<DxccMatch?> MatchAsync(AF0E.Services.DxCluster.Models.DxClusterSpot spot, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(spot);

        if (string.IsNullOrWhiteSpace(spot.DxCallsign))
            return null;

        var snapshot = await GetSnapshotAsync(cancellationToken);
        var match = snapshot.Match(NormalizeCallsign(spot.DxCallsign));
        if (match is null)
            return null;

        var workedStatus = await GetWorkedStatusAsync(match.EntityCode, spot, cancellationToken);
        return match with { WorkedStatus = workedStatus };
    }

    public ValueTask DisposeAsync()
    {
        _reloadGate.Dispose();
        return ValueTask.CompletedTask;
    }

    private async ValueTask<DxccWorkedStatus> GetWorkedStatusAsync(int entityCode, AF0E.Services.DxCluster.Models.DxClusterSpot spot, CancellationToken cancellationToken)
    {
        var band = RadioHelper.DetectBand(spot.FrequencyKhz);
        var mode = RadioHelper.NormalizeMode(spot.Mode);
        var cacheKey = new StatusCacheKey(entityCode, band, mode);

        lock (_statusCacheSync)
        {
            if (_statusCache.TryGetValue(cacheKey, out var cached)
                && DateTimeOffset.UtcNow - cached.CreatedAtUtc < StatusCacheLifetime)
            {
                return cached.Status;
            }
        }

        var status = await QueryWorkedStatusAsync(entityCode, band, mode, cancellationToken);

        lock (_statusCacheSync)
        {
            _statusCache[cacheKey] = new StatusCacheEntry(status, DateTimeOffset.UtcNow);
        }

        return status;
    }

    private async ValueTask<DxccWorkedStatus> QueryWorkedStatusAsync(int entityCode, string? band, string? mode, CancellationToken cancellationToken)
    {
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<HrdDbContext>();
        var entityCodeText = entityCode.ToString();

        var records = await dbContext.Log
            .AsNoTracking()
            .Where(log => log.ColDxcc == entityCodeText)
            .Select(log => new WorkedLogRow(log.ColBand, log.ColMode, log.ColSubmode, log.ColQslRcvd, log.ColLotwQslRcvd))
            .ToListAsync(cancellationToken);

        if (records.Count == 0)
            return DxccWorkedStatus.NotWorked;

        var hasVerifiedAny = false;

        foreach (var record in records)
        {
            var verified = IsVerified(record.QslRcvd) || IsVerified(record.LotwQslRcvd);
            var logBand = RadioHelper.NormalizeBand(record.Band);
            var logMode = NormalizeLogMode(record.Mode, record.Submode);

            if (!verified)
                continue;

            hasVerifiedAny = true;

            if (!string.IsNullOrWhiteSpace(band)
                && !string.IsNullOrWhiteSpace(mode)
                && string.Equals(logBand, band, StringComparison.OrdinalIgnoreCase)
                && RadioHelper.ModesMatch(logMode, mode))
            {
                return DxccWorkedStatus.VerifiedBandMode;
            }
        }

        return hasVerifiedAny ? DxccWorkedStatus.VerifiedOtherBandMode : DxccWorkedStatus.WorkedNotVerified;
    }

    private async Task<Snapshot> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var snapshot = _snapshot;
        if (snapshot.Entries.Count > 0 && DateTimeOffset.UtcNow - _loadedAtUtc < CacheLifetime)
            return snapshot;

        await _reloadGate.WaitAsync(cancellationToken);
        try
        {
            snapshot = _snapshot;
            if (snapshot.Entries.Count > 0 && DateTimeOffset.UtcNow - _loadedAtUtc < CacheLifetime)
                return snapshot;

            await using var scope = serviceScopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<HrdDbContext>();
            var rows = await dbContext.Dxcc
                .AsNoTracking()
                .Where(row => row.PrefixRegExp != null && row.PrefixRegExp != string.Empty)
                .Where(row => row.Deleted != "Y")
                .Select(row => new DxccRow(row.EntityCode, row.DisplayName, row.PrefixRegExp!, row.Prefix, row.CountryCode))
                .ToListAsync(cancellationToken);

            var entries = new List<Entry>(rows.Count);
            foreach (var row in rows)
            {
                try
                {
                    entries.Add(Entry.Create(row, RegexTimeout));
                }
                catch (ArgumentException ex)
                {
                    LogInvalidDxccRegex(row.EntityCode, row.DisplayName, row.PrefixRegExp, ex);
                }
            }

            entries.Sort(EntryComparer.Instance);

            _snapshot = new Snapshot([.. entries]);
            _loadedAtUtc = DateTimeOffset.UtcNow;
            LogDxccEntriesLoaded(entries.Count);
            return _snapshot;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            snapshot = _snapshot;
            if (snapshot.Entries.Count > 0)
            {
                LogDxccRefreshFailedUsingCachedData(ex);
                return snapshot;
            }

            LogDxccRefreshFailed(ex);
            return Snapshot.Empty;
        }
        finally
        {
            _reloadGate.Release();
        }
    }

    private static string NormalizeCallsign(string callsign)
        => new([.. callsign.Trim().ToUpperInvariant().Where(static ch => !char.IsWhiteSpace(ch))]);

    private static string? NormalizeLogMode(string? mode, string? submode)
        => RadioHelper.NormalizeMode(string.IsNullOrWhiteSpace(submode) ? mode : submode);

    private static bool IsVerified(string? value)
        => string.Equals(value?.Trim(), "V", StringComparison.OrdinalIgnoreCase);

    [LoggerMessage(Level = LogLevel.Information, Message = "Loaded {EntryCount} DXCC prefix regex entries")]
    private partial void LogDxccEntriesLoaded(int entryCount);

    [LoggerMessage(Level = LogLevel.Warning, Message = "DXCC regex refresh failed; continuing with cached entries")]
    private partial void LogDxccRefreshFailedUsingCachedData(Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "DXCC regex refresh failed")]
    private partial void LogDxccRefreshFailed(Exception exception);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Skipping invalid DXCC prefix regex for entity {EntityCode} ({EntityName}): {Pattern}")]
    private partial void LogInvalidDxccRegex(int entityCode, string entityName, string pattern, Exception exception);

    private sealed record DxccRow(int EntityCode, string DisplayName, string PrefixRegExp, string? Prefix, string? CountryCode);

    private sealed record WorkedLogRow(string? Band, string? Mode, string? Submode, string? QslRcvd, string? LotwQslRcvd);

    private sealed record Entry(
        int EntityCode,
        string EntityName,
        string? CountryCode,
        string PrefixRegExp,
        Regex Regex,
        int LiteralPrefixLength,
        int PrefixTokenLength,
        int RegexSpecificityScore)
    {
        public static Entry Create(DxccRow row, TimeSpan regexTimeout)
        {
            var literalPrefix = ExtractLiteralPrefix(row.PrefixRegExp);

            return new Entry(
                row.EntityCode,
                row.DisplayName,
                NormalizeCountryCode(row.CountryCode),
                row.PrefixRegExp,
                new Regex(row.PrefixRegExp, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, regexTimeout),
                literalPrefix.Length,
                GetPrefixTokenLength(row.Prefix),
                GetRegexSpecificityScore(row.PrefixRegExp));
        }

        public bool IsMatch(string callsign)
        {
            try
            {
                return Regex.IsMatch(callsign);
            }
            catch (RegexMatchTimeoutException)
            {
                return false;
            }
        }

        public DxccMatch ToMatch()
            => new()
            {
                EntityCode = EntityCode,
                EntityName = EntityName,
                CountryCode = CountryCode,
                WorkedStatus = DxccWorkedStatus.NotWorked
            };

        private static string? NormalizeCountryCode(string? countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
                return null;

            var normalized = countryCode.Trim().ToUpperInvariant();
            if (normalized.Length != 2 || !normalized.All(char.IsLetter))
                return null;

            return normalized is "ZZ" ? null : normalized;
        }

        private static string ExtractLiteralPrefix(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return string.Empty;

            var span = pattern.AsSpan().Trim();
            var result = new StringBuilder();

            var index = 0;
            if (index < span.Length && span[index] == '^')
                index++;

            while (index < span.Length)
            {
                var ch = span[index];
                if (char.IsLetterOrDigit(ch))
                {
                    result.Append(char.ToUpperInvariant(ch));
                    index++;
                    continue;
                }

                if (ch == '\\' && index + 1 < span.Length && char.IsLetterOrDigit(span[index + 1]))
                {
                    result.Append(char.ToUpperInvariant(span[index + 1]));
                    index += 2;
                    continue;
                }

                break;
            }

            return result.ToString();
        }

        private static int GetPrefixTokenLength(string? prefix)
        {
            if (string.IsNullOrWhiteSpace(prefix))
                return 0;

            return prefix
                .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(static token => token.Count(char.IsLetterOrDigit))
                .DefaultIfEmpty(0)
                .Max();
        }

        private static int GetRegexSpecificityScore(string pattern)
        {
            if (string.IsNullOrWhiteSpace(pattern))
                return 0;

            var score = 0;
            foreach (var ch in pattern)
            {
                if (char.IsLetterOrDigit(ch))
                    score += 4;
                else switch (ch)
                {
                    case '[' or ']':
                        score += 1;
                        break;
                    case '^' or '$':
                        score += 0;
                        break;
                    default:
                        score -= 1;
                        break;
                }
            }

            return score;
        }
    }

    private sealed class EntryComparer : IComparer<Entry>
    {
        public static EntryComparer Instance { get; } = new();

        public int Compare(Entry? x, Entry? y)
        {
            if (ReferenceEquals(x, y))
                return 0;

            if (x is null)
                return 1;

            if (y is null)
                return -1;

            var byLiteralPrefix = y.LiteralPrefixLength.CompareTo(x.LiteralPrefixLength);
            if (byLiteralPrefix != 0)
                return byLiteralPrefix;

            var byPrefixTokenLength = y.PrefixTokenLength.CompareTo(x.PrefixTokenLength);
            if (byPrefixTokenLength != 0)
                return byPrefixTokenLength;

            var bySpecificity = y.RegexSpecificityScore.CompareTo(x.RegexSpecificityScore);
            if (bySpecificity != 0)
                return bySpecificity;

            var byPatternLength = y.PrefixRegExp.Length.CompareTo(x.PrefixRegExp.Length);
            return byPatternLength != 0 ? byPatternLength : x.EntityCode.CompareTo(y.EntityCode);
        }
    }

    private sealed class Snapshot(IReadOnlyList<Entry> entries)
    {
        public static Snapshot Empty { get; } = new([]);

        public IReadOnlyList<Entry> Entries { get; } = entries;

        public DxccMatch? Match(string callsign)
            => Entries.FirstOrDefault(entry => entry.IsMatch(callsign))?.ToMatch();
    }

    private readonly record struct StatusCacheKey(int EntityCode, string? Band, string? Mode);

    private readonly record struct StatusCacheEntry(DxccWorkedStatus Status, DateTimeOffset CreatedAtUtc);
}
