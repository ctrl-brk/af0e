using AF0E.DB.Models;

namespace Logbook.Api.Models;

public sealed class PotaActivationQsoSummary(PotaContact contact)
{
    public int LogId { get; set; } = contact.Log.ColPrimaryKey;
    public string Band { get; set; } = contact.Log.ColBand!;
    public string Call { get; set; } = contact.Log.ColCall;
    public int? Cqz { get; set; } = (int?)contact.Log.ColCqz;
    public DateTime Date { get; set; } = contact.Log.ColTimeOn!.Value;
    public string? Dxcc { get; set; } = contact.Log.ColDxcc;
    public double? Freq { get; set; } = contact.Log.ColFreq;
    public string? Grid { get; set; } = contact.Log.ColGridsquare;
    public int? Ituz { get; set; } = (int?)contact.Log.ColItuz;
    public decimal? Lat { get; set; } = contact.Lat;
    public decimal? Lon { get; set; } = contact.Long;
    public string? Mode { get; set; } = contact.Log.ColMode;
    public string? MyCity { get; set; } = contact.Log.ColMyCity;
    public string? MyCountry { get; set; } = contact.Log.ColMyCountry;
    public string? MyCnty { get; set; } = contact.Log.ColMyCnty;
    public string? MyGrid { get; set; } = contact.Log.ColMyGridsquare;
    public string? MyState { get; set; } = contact.Log.ColMyState;
    public string? RstRcvd { get; set; } = contact.Log.ColRstRcvd;
    public string? RstSent { get; set; } = contact.Log.ColRstSent;
    public string? State { get; set; } = contact.Log.ColState;
    public string? StationCallsign { get; set; } = contact.Log.ColStationCallsign;
    public string? OperatorCallsign { get; set; } = contact.Log.ColOperator;
#pragma warning disable CA1819 // Properties should not return arrays
    public string[] p2p { get; set; } = [.. contact.Log.PotaHunting.Select(p => p.Park.ParkNum)];
    public string? SatName { get; set; } = contact.Log.ColSatName;
}
