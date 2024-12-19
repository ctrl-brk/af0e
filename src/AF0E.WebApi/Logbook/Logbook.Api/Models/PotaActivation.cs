namespace Logbook.Api.Models;

internal sealed class PotaActivation
{
    public int ActivationId { get; init; }
    public int ParkId { get; init; }
    public string Grid { get; init; } = null!;
    public string? City { get; init; }
    public string County { get; init; } = null!;
    public string State { get; init; } = null!;
    public DateTime StartDate { get; init; }
    public DateTime? EndDate { get; init; }
    public DateTime? LogSubmittedDate { get; init; }
    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public string? SiteComments { get; init; }
    public string? Comments { get; init; }
    public decimal Lat { get; init; }
    public decimal Long { get; init; }

    public PotaPark Park { get; init; } = null!;
    // ReSharper disable once CollectionNeverUpdated.Global
    public ICollection<PotaContact> PotaContacts { get; init; } = [];
}
