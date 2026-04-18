using System.Text.Json.Serialization;
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
        Name = log.ColName;
        State = log.ColState;
        County = log.ColCnty;
        Country = log.ColCountry;
        Grid = log.ColGridsquare;
        CqZone = (int?)log.ColCqz;
        ItuZone = (int?)log.ColItuz;
        Dxcc = !string.IsNullOrEmpty(log.ColDxcc) ? int.Parse(log.ColDxcc) : null;
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
        p2p = log.PotaHunting.Count > 0;
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
    [JsonPropertyName("name_fmt")]
    public string? Name { get; set; }
    public string? County { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; }
    public string? Grid { get; set; }
    public int? CqZone { get; set; }
    public int? ItuZone { get; set; }
    public int? Dxcc { get; set; }
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
            ColRstSent = N(RstSent),
            ColRstRcvd = N(RstRcvd),
            ColName = N(Name),
            ColCnty = N(County),
            ColState = N(State)?.ToUpperInvariant(),
            ColCountry = N(Country),
            ColGridsquare = N(Grid),
            ColCqz = N(CqZone, 0),
            ColItuz = ItuZone,
            ColDxcc = Dxcc?.ToString(),
            ColEqslQslRcvd = "N",
            ColEqslQslSent = "N",
            ColForceInit = null,
            ColLotwQslRcvd = "N",
            ColLotwQslSent = "N",
            ColMyCity = N(MyCity),
            ColMyCnty = N(MyCounty),
            ColMyState = N(MyState),
            ColMyCountry = N(MyCountry),
            ColMyCqZone = MyCqZone,
            ColMyItuZone = MyItuZone,
            ColMyGridsquare = N(MyGrid),
            ColQslSent = N(QslSent, "N"),
            ColQslsdate = QslSentDate,
            ColQslSentVia = N(QslSentVia, "D"),
            ColQslRcvd = N(QslRcvd, "N"),
            ColQslrdate = QslRcvdDate,
            ColQslRcvdVia = N(QslRcvdVia, "D"),
            ColQsoRandom = null,
            ColRxPwr = 0,
            ColSwl = 1,
            SiteComment = N(SiteComment),
            ColComment = includeAdminFields ? N(Comment) : null
        };
    }

    private static string? N(string? value, string? def = null) => string.IsNullOrWhiteSpace(value) ? def : value;
    private static double? N(double? value, double? def = null) => value ?? def;
}
