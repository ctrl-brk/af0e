using AF0E.DB.Models;

namespace Logbook.Api.Models;

public sealed class PotaHuntingQsoSummary(PotaHunting contact)
{
    public int Id { get; set; } = contact.Log.ColPrimaryKey;
    public DateTime Date { get; set; } = contact.Log.ColTimeOn!.Value;
    public string Call { get; set; } = contact.Log.ColCall;
    public string? Band { get; set; } = contact.Log.ColBand;
    public string? Mode { get; set; } = contact.Log.ColMode;
    public string? Grid { get; set; } = contact.Log.ColGridsquare;
    public string? SatName { get; set; } = contact.Log.ColSatName;
    public int PotaCount { get; set; } = contact.Log.PotaContacts.Count;
}
