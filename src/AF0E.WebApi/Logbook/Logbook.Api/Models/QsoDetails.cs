using AF0E.DB.Models;

namespace Logbook.Api.Models;

public sealed class QsoDetails(HrdLog log)
{
    public int Id { get; set; } = log.ColPrimaryKey;
    public DateTime Date { get; set; } = log.ColTimeOn!.Value;
    public string Call { get; set; } = log.ColCall;
    public string Band { get; set; } = log.ColBand!;
    public string? BandRx { get; set; } = log.ColBandRx;
    public double? Freq { get; set; } = log.ColFreq!;
    public double? FreqRx { get; set; } = log.ColFreqRx;
    public string Mode { get; set; } = log.ColMode!;
    public string? RstSent { get; set; } = log.ColRstSent;
    public string? RstRcvd { get; set; } = log.ColRstRcvd;
    public string? MyCity { get; set; } = log.ColMyCity;
    public string? MyCounty { get; set; } = log.ColMyCnty;
    public string? MyState { get; set; } = log.ColMyState;
    public string? MyCountry { get; set; } = log.ColMyCountry;
    public double? MyCqZone { get; set; } = log.ColMyCqZone;
    public double? MyItuZone { get; set; } = log.ColMyItuZone;
    public string? MyGrid { get; set; } = log.ColMyGridsquare;
    public string? QslSent { get; set; } = log.ColQslSent;
    public DateTime? QslSentDate { get; set; } = log.ColQslsdate;
    public string? QslSentVia { get; set; } = log.ColQslSentVia;
    public string? QslRcvd { get; set; } = log.ColQslRcvd;
    public DateTime? QslRcvdDate { get; set; } = log.ColQslrdate;
    public string? QslRcvdVia { get; set; } = log.ColQslRcvdVia;
    public List<string> POTA { get; set; } = [.. log.PotaContacts.Select(x => x.Activation.Park.ParkNum)];
    public bool p2p { get; set; } = log.PotaContacts.Count > 0 && !string.IsNullOrEmpty(log.PotaContacts.First().P2P);
    public string? SatName { get; set; } = log.ColSatName;
    public string? SatMode { get; set; } = log.ColSatMode;
    public string? Contest { get; set; } = log.ColContestId;
    public string? Comment { get; set; } = log.SiteComment;
}
