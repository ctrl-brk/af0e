using AF0E.Services.Pota.Models;

namespace AF0E.Services.Pota.Extensions;

public static class PotaActivityInfoExtensions
{
    /// <summary>
    /// Filters spots by activator call sign (case-insensitive)
    /// </summary>
    public static IEnumerable<PotaActivityInfo> WithActivator(this IEnumerable<PotaActivityInfo> spots, string callSign)
        => spots.Where(s => string.Equals(s.CallSign, callSign, StringComparison.OrdinalIgnoreCase));

    /// <summary>
    /// Filters out spots with frequency higher than the specified maximum (in kHz)
    /// </summary>
    public static IEnumerable<PotaActivityInfo> WithMaxFrequency(this IEnumerable<PotaActivityInfo> spots, decimal maxFreqKhz)
        => spots.Where(s => 
        {
            if (string.IsNullOrWhiteSpace(s.FreqKhz))
                return true;

            if (!decimal.TryParse(s.FreqKhz, out var freq))
                return true;

            return freq <= maxFreqKhz;
        });
}
