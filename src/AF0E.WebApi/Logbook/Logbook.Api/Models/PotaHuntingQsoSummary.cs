using System.Diagnostics.CodeAnalysis;
using AF0E.DB.Models;

namespace Logbook.Api.Models;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public sealed class PotaHuntingQsoSummary(PotaHunting contact)
{
    public int Id { get; } = contact.Log.ColPrimaryKey;
    public DateTime Date { get; } = contact.Log.ColTimeOn!.Value;
    public string Call { get; } = contact.Log.ColCall;
    public string? Band { get; } = contact.Log.ColBand;
    public string? Mode { get; } = contact.Log.ColMode;
    public string? Grid { get; } = contact.Log.ColGridsquare;
    public string? SatName { get; } = contact.Log.ColSatName;
    public int PotaCount { get; } = contact.Log.PotaContacts.Count;
}
