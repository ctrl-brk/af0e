using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
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
    public int TotalActivationCount { get; set; }
    [Column("QSOs")]
    public int TotalQsoCount { get; set; }
    public bool Active { get; init; }
    [DatabaseGenerated(DatabaseGeneratedOption.Computed), JsonIgnore]
    public Geometry GeoPoint { get; init; } = null!;

    [JsonIgnore]
    public ICollection<PotaActivation> PotaActivations { get; init; } = [];
    [JsonIgnore]
    public ICollection<PotaHunting> PotaHunting { get; init; } = [];
}
