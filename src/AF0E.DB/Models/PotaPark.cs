using System.ComponentModel.DataAnnotations.Schema;
using NetTopologySuite.Geometries;

namespace AF0E.DB.Models;

public sealed class PotaPark
{
    public int ParkId { get; init; }
    public string ParkNum { get; init; } = null!;
    public string ParkName { get; init; } = null!;
    public decimal? Lat { get; init; }
    public decimal? Long { get; init; }
    public string? Grid { get; init; }
    public string? Location { get; init; }
    public string Country { get; init; } = null!;
    [Column("Activations")]
    public int TotalActivationCount { get; init; }
    [Column("QSOs")]
    public int TotalQsoCount { get; init; }
    public bool Active { get; init; }
    public Geometry GeoPoint { get; init; } = null!;

    // ReSharper disable once CollectionNeverUpdated.Global
    public ICollection<PotaActivation> PotaActivations { get; init; } = [];
}
