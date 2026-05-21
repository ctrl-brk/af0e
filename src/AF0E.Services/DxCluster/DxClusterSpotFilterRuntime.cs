using System.Text.RegularExpressions;
using AF0E.Common.Radio;
using AF0E.Services.DxCluster.Configuration;
using AF0E.Services.DxCluster.Models;

namespace AF0E.Services.DxCluster;

internal sealed class DxClusterSpotFilterRuntime
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(250);

    private readonly Regex[] _callsignRegexes;
    private readonly HashSet<string> _modes;
    private readonly FrequencyWindow[] _frequencyWindows;

    private DxClusterSpotFilterRuntime(DxClusterSpotFilter definition, Regex[] callsignRegexes, HashSet<string> modes, FrequencyWindow[] frequencyWindows)
    {
        Definition = definition;
        _callsignRegexes = callsignRegexes;
        _modes = modes;
        _frequencyWindows = frequencyWindows;
    }

    public DxClusterSpotFilter Definition { get; }

    public static DxClusterSpotFilterRuntime Create(DxClusterFilterOptions options, string name)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var callsignPatterns = SplitPipeDelimited(options.CallsignPatterns);
        var compiledRegexes = new List<Regex>(callsignPatterns.Length);
        var invalidPatterns = new List<string>();

        foreach (var pattern in callsignPatterns)
        {
            try
            {
                compiledRegexes.Add(new Regex($"^(?:{pattern})$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, RegexTimeout));
            }
            catch (ArgumentException)
            {
                invalidPatterns.Add(pattern);
            }
        }

        var modes = (options.Modes ?? [])
            .Select(DxClusterSpotModeDetector.NormalizeMode)
            .Where(static mode => !string.IsNullOrWhiteSpace(mode))
            .Select(static mode => mode!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var frequencyWindows = (options.FrequencyWindows ?? [])
            .Select(static window => FrequencyWindow.Create(window.MinFrequencyKhz, window.MaxFrequencyKhz))
            .Where(static window => window is not null)
            .Select(static window => window!.Value)
            .ToArray();

        return new DxClusterSpotFilterRuntime(
            new DxClusterSpotFilter
            {
                Name = name,
                CallsignPatterns = callsignPatterns.Length > 0 ? string.Join('|', callsignPatterns) : null,
                Modes = modes.Length > 0 ? modes : null,
                FrequencyWindows = frequencyWindows.Length > 0 ? frequencyWindows.Select(static window => window.ToModel()).ToArray() : null,
                InvalidCallsignPatterns = invalidPatterns
            },
            [.. compiledRegexes],
            [.. modes],
            frequencyWindows);
    }

    public bool IsMatch(DxClusterSpot spot)
    {
        ArgumentNullException.ThrowIfNull(spot);

        if (_callsignRegexes.Length > 0 && !_callsignRegexes.Any(regex => IsRegexMatch(regex, spot.DxCallsign)))
            return false;

        if (_frequencyWindows.Length > 0 && !_frequencyWindows.Any(window => window.Contains(spot.FrequencyKhz)))
            return false;

        if (_modes.Count <= 0)
            return true;

        var spotMode = DxClusterSpotModeDetector.NormalizeMode(spot.Mode);
        return _modes.Any(filterMode => RadioHelper.ModesMatch(filterMode, spotMode));
    }

    private static bool IsRegexMatch(Regex regex, string value)
    {
        try
        {
            return regex.IsMatch(value);
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private static string[] SplitPipeDelimited(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? []
            : value.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

    private readonly record struct FrequencyWindow(decimal? MinFrequencyKhz, decimal? MaxFrequencyKhz)
    {
        public static FrequencyWindow? Create(decimal? minFrequencyKhz, decimal? maxFrequencyKhz)
        {
            if (minFrequencyKhz is null && maxFrequencyKhz is null)
                return null;

            if (minFrequencyKhz is not null && maxFrequencyKhz is not null && minFrequencyKhz > maxFrequencyKhz)
                (minFrequencyKhz, maxFrequencyKhz) = (maxFrequencyKhz, minFrequencyKhz);

            return new FrequencyWindow(minFrequencyKhz, maxFrequencyKhz);
        }

        public bool Contains(decimal frequencyKhz)
            => (MinFrequencyKhz is null || frequencyKhz >= MinFrequencyKhz.Value)
               && (MaxFrequencyKhz is null || frequencyKhz <= MaxFrequencyKhz.Value);

        public DxClusterFrequencyWindow ToModel()
            => new()
            {
                MinFrequencyKhz = MinFrequencyKhz,
                MaxFrequencyKhz = MaxFrequencyKhz
            };
    }
}
