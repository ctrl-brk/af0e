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
#pragma warning disable CA1819
    public string[] p2p { get; set; } = contact.P2P == null ? [] : contact.P2P.Split(',', StringSplitOptions.None);
    public decimal? Lat { get; set; } = contact.Lat;
#pragma warning disable CA1720
    public decimal? Long { get; set; } = contact.Long;
}
