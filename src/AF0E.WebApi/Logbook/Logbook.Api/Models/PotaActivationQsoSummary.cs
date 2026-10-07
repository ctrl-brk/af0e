using System.Diagnostics.CodeAnalysis;
using AF0E.DB.Models;

namespace Logbook.Api.Models;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public sealed class PotaActivationQsoSummary(PotaContact contact)
{
    public int LogId { get; } = contact.Log.ColPrimaryKey;
    public string Band { get; } = contact.Log.ColBand!;
    public string Call { get; } = contact.Log.ColCall;
    public int? Cqz { get; } = (int?)contact.Log.ColCqz;
    public DateTime Date { get; } = contact.Log.ColTimeOn!.Value;
    public string? Dxcc { get; } = contact.Log.ColDxcc;
    public double? Freq { get; } = contact.Log.ColFreq;
    public string? Grid { get; } = contact.Log.ColGridsquare;
    public int? Ituz { get; } = (int?)contact.Log.ColItuz;
    public decimal? Lat { get; } = contact.Lat;
    public decimal? Lon { get; } = contact.Long;
    public string? Mode { get; } = contact.Log.ColMode;
    public string? MyCity { get; } = contact.Log.ColMyCity;
    public string? MyCountry { get; } = contact.Log.ColMyCountry;
    public string? MyCnty { get; } = contact.Log.ColMyCnty;
    public string? MyGrid { get; } = contact.Log.ColMyGridsquare;
    public string? MyState { get; } = contact.Log.ColMyState;
    public string? RstRcvd { get; } = contact.Log.ColRstRcvd;
    public string? RstSent { get; } = contact.Log.ColRstSent;
    public string? State { get; } = contact.Log.ColState;
    public string? StationCallsign { get; } = contact.Log.ColStationCallsign;
    public string? OperatorCallsign { get; } = contact.Log.ColOperator;
    // ReSharper disable once InconsistentNaming
    public IReadOnlyList<string> p2p { get; } = [.. contact.Log.PotaHunting.Select(p => p.Park.ParkNum)];
    public string? SatName { get; } = contact.Log.ColSatName;
}
