using System.Diagnostics.CodeAnalysis;
using AF0E.DB.Models;

namespace Logbook.Api.Models;

[SuppressMessage("ReSharper", "UnusedMember.Global")]
public sealed class GridTrackerLookup(HrdLog log)
{
    public int Id { get; } = log.ColPrimaryKey;
    public string Call { get; } = log.ColCall;
    public DateTime Date { get; } = log.ColTimeOn!.Value;
    public string? Mode { get; } = log.ColMode;
    public string? Band { get; } = log.ColBand;
    public string? Comment { get; } = log.ColComment;
    public string? Grid { get; } = log.ColGridsquare;
    public DateTime? Qslsdate { get; } = log.ColQslsdate;
    public string? QslSentVia { get; } = log.ColQslSentVia;
    public string? QslRcvd { get; } = log.ColQslRcvd;
    public string? LotwQslRcvd { get; } = log.ColLotwQslRcvd;
    public IReadOnlyList<string> Parks { get; } = [.. log.PotaHunting.Select(p => p.Park.ParkNum)];
}
