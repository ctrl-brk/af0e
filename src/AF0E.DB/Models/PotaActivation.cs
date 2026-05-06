// ReSharper disable PropertyCanBeMadeInitOnly.Global
namespace AF0E.DB.Models;

public sealed class PotaActivation
{
    public int ActivationId { get; init; }
    public int ParkId { get; set; }
    public string Grid { get; set; } = null!;
    public string? City { get; set; }
    public string County { get; set; } = null!;
    public string State { get; set; } = null!;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? LogSubmittedDate { get; set; }
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string? SiteComments { get; set; }
    public string? Comments { get; set; }
    public decimal Lat { get; set; }
    public decimal Long { get; set; }
    public char Status { get; set; }
    public string StationCallsign { get; set; } = null!;
    public string OperatorCallsign { get; set; } = null!;

    public PotaPark Park { get; init; } = null!;
    // ReSharper disable once CollectionNeverUpdated.Global
    public ICollection<PotaContact> PotaContacts { get; init; } = [];
}
