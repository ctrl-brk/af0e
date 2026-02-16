using AF0E.Services.Pota.Models;

namespace AF0E.Services.Pota.Extensions;

public static class PotaSpotExtensions
{
    /// <summary>
    /// Filters spots to only active (not QRT)
    /// </summary>
    public static IEnumerable<PotaSpot> ActiveOnly(this IEnumerable<PotaSpot> spots)
        => spots.Where(s => !s.Qrt);

    /// <summary>
    /// Filters spots by ham radio band (e.g., "20m", "40m")
    /// </summary>
    public static IEnumerable<PotaSpot> OnBand(this IEnumerable<PotaSpot> spots, string band)
        => spots.Where(s => s.Frequency.IsOnBand(band));
}
