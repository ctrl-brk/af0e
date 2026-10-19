using System.Diagnostics.CodeAnalysis;
using AF0E.Services.Pota.Models;

namespace Logbook.Api.Models;

/// <summary>
/// POTA activity information enriched with contact statistics
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public sealed record PotaActivityWithStats
{
    public required PotaActivityInfo Activity { get; init; }

    /// <summary>
    /// Number of contacts with this park, grouped by band and mode
    /// </summary>
    public IReadOnlyList<ParkContactStats> ParkContactsByBandMode { get; init; } = [];

    /// <summary>
    /// Total number of contacts with this park (across all bands/modes)
    /// </summary>
    public int TotalParkContacts { get; init; }

    /// <summary>
    /// Total number of contacts with this call sign (across all parks)
    /// </summary>
    public int TotalCallSignContacts { get; init; }
}

/// <summary>
/// Contact statistics for a park grouped by band and mode
/// </summary>
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public sealed record ParkContactStats
{
    public required string Band { get; init; }
    public required string Mode { get; init; }
    public int Count { get; init; }
}
