using AF0E.DB.Models;

namespace Logbook.Api.Models;

public sealed class QsoDetails
{
    // ReSharper disable once UnusedMember.Global
    public QsoDetails() {}

    public QsoDetails(HrdLog log, bool isAdmin = false)
    {
        Id = log.ColPrimaryKey;
        Date = log.ColTimeOn!.Value;
        Call = log.ColCall;
        Band = log.ColBand!;
        BandRx = log.ColBandRx;
        Freq = log.ColFreq;
        FreqRx = log.ColFreqRx;
        Mode = log.ColMode!;
        RstSent = log.ColRstSent;
        RstRcvd = log.ColRstRcvd;
        MyCity = log.ColMyCity;
        MyCounty = log.ColMyCnty;
        MyState = log.ColMyState;
        MyCountry = log.ColMyCountry;
        MyCqZone = log.ColMyCqZone;
        MyItuZone = log.ColMyItuZone;
        MyGrid = log.ColMyGridsquare;
        QslSent = log.ColQslSent;
        QslSentDate = log.ColQslsdate;
        QslSentVia = log.ColQslSentVia;
        QslRcvd = log.ColQslRcvd;
        QslRcvdDate = log.ColQslrdate;
        QslRcvdVia = log.ColQslRcvdVia;
        POTA = [.. log.PotaContacts.Select(x => x.Activation.Park.ParkNum)];
        p2p = log.PotaContacts.Count > 0 && !string.IsNullOrEmpty(log.PotaContacts.First().P2P);
        SatName = log.ColSatName;
        SatMode = log.ColSatMode;
        Contest = log.ColContestId;
        SiteComment = log.SiteComment;
        Comment = isAdmin ? log.ColComment : null;
    }

    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string Call { get; set; } = string.Empty;
    public string Band { get; set; } = string.Empty;
    public string? BandRx { get; set; }
    public double? Freq { get; set; }
    public double? FreqRx { get; set; }
    public string Mode { get; set; } = string.Empty;
    public string? RstSent { get; set; }
    public string? RstRcvd { get; set; }
    public string? MyCity { get; set; }
    public string? MyCounty { get; set; }
    public string? MyState { get; set; }
    public string? MyCountry { get; set; }
    public double? MyCqZone { get; set; }
    public double? MyItuZone { get; set; }
    public string? MyGrid { get; set; }
    public string? QslSent { get; set; }
    public DateTime? QslSentDate { get; set; }
    public string? QslSentVia { get; set; }
    public string? QslRcvd { get; set; }
    public DateTime? QslRcvdDate { get; set; }
    public string? QslRcvdVia { get; set; }
    public List<string> POTA { get; set; } = [];
    public bool p2p { get; set; }
    public string? SatName { get; set; }
    public string? SatMode { get; set; }
    public string? Contest { get; set; }
    public string? SiteComment { get; set; }
    // Admin-only fields
    public string? Comment { get; set; }

    /// <summary>
    /// Converts this QsoDetails to a new HrdLog entity
    /// </summary>
    /// <param name="includeAdminFields">Whether to include admin-only fields (like Comment)</param>
    /// <returns>New HrdLog entity ready to be added to the database</returns>
    public HrdLog ToHrdLog(bool includeAdminFields = false)
    {
        return new HrdLog
        {
            ColCall = Call.ToUpperInvariant(),
            ColTimeOn = Date,
            ColBand = Band,
            ColFreq = Freq,
            ColMode = Mode,
            ColRstSent = RstSent,
            ColRstRcvd = RstRcvd,
            ColMyCity = MyCity,
            ColMyCnty = MyCounty,
            ColMyState = MyState,
            ColMyCountry = MyCountry,
            ColMyCqZone = MyCqZone,
            ColMyItuZone = MyItuZone,
            ColMyGridsquare = MyGrid,
            ColQslSent = QslSent,
            ColQslsdate = QslSentDate,
            ColQslSentVia = QslSentVia,
            ColQslRcvd = QslRcvd,
            ColQslrdate = QslRcvdDate,
            ColQslRcvdVia = QslRcvdVia,
            SiteComment = SiteComment,
            ColComment = includeAdminFields ? Comment : null
        };
    }
}
