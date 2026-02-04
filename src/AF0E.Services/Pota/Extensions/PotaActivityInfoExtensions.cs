using AF0E.Services.Pota.Models;

namespace AF0E.Services.Pota.Extensions;

public static class PotaActivityInfoExtensions
{
    /// <summary>
    /// Filters spots by activator call sign (case-insensitive)
    /// </summary>
    public static IEnumerable<PotaActivityInfo> WithActivator(this IEnumerable<PotaActivityInfo> spots, string callSign)
        => spots.Where(s => string.Equals(s.CallSign, callSign, StringComparison.OrdinalIgnoreCase));
}
