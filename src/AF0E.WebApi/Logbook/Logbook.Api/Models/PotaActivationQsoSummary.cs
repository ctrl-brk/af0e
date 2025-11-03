using AF0E.DB.Models;

namespace Logbook.Api.Models;

public sealed class PotaActivationQsoSummary(PotaContact contact)
{
    public int LogId { get; set; } = contact.Log.ColPrimaryKey;
    public DateTime Date { get; set; } = contact.Log.ColTimeOn!.Value;
    public string Call { get; set; } = contact.Log.ColCall;
    public string Band { get; set; } = contact.Log.ColBand!;
    public string? Mode { get; set; } = contact.Log.ColMode;
    public string? SatName { get; set; } = contact.Log.ColSatName;
    public string[] p2p { get; set; } = contact.P2P == null ? Array.Empty<string>() : contact.P2P.Split(',', StringSplitOptions.None);
    public decimal? Lat { get; set; } = contact.Lat;
    public decimal? Long { get; set; } = contact.Long;
}
