using AF0E.DB.Models;

namespace Logbook.Api.Models;

public class GridTrackerLookup(HrdLog log)
{
    public int Id { get; set; } = log.ColPrimaryKey;
    public string Call { get; set; } = log.ColCall;
    public DateTime Date { get; set; } = log.ColTimeOn!.Value;
    public string? Mode { get; set; } = log.ColMode;
    public string? Band { get; set; } = log.ColBand;
    public string? Comment { get; set; } = log.ColComment;
    public string? Grid { get; set; } = log.ColGridsquare;
    public DateTime? Qslsdate { get; set; } = log.ColQslsdate;
    public string? QslSentVia { get; set; } = log.ColQslSentVia;
    public string? QslRcvd { get; set; } = log.ColQslRcvd;
    public string? LotwQslRcvd { get; set; } = log.ColLotwQslRcvd;
    public IEnumerable<string> Parks { get; set; } = log.PotaHunting.Select(p => p.Park.ParkNum);
}
