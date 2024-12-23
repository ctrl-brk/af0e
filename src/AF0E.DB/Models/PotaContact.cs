using System.Diagnostics.CodeAnalysis;

namespace AF0E.DB.Models;

#pragma warning disable CA1720 // Identifier contains type name (Long)
[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
public sealed class PotaContact
{
    public int ContactId { get; init; }
    public int LogId { get; init; }
    public int ActivationId { get; init; }
    public string? P2P { get; init; }
    public decimal? Lat { get; set; }
    public decimal? Long { get; set; }
    public DateTime? QrzLookupDate { get; set; }
    public string? QrzGeoLoc { get; set; }

    public PotaActivation Activation { get; init; } = null!;
    public HrdLog Log { get; init; } = null!;
}
