using AF0E.DB.Models;

namespace Logbook.Api.Models;

public sealed class QsoSummary(HrdLog log)
{
    public QsoSummary(HrdLog log, string metadata) : this(log)
    {
        Metadata = metadata;
    }
    public int Id { get; set; } = log.ColPrimaryKey;
    public DateTime Date { get; set; } = log.ColTimeOn!.Value;
    public string Call { get; set; } = log.ColCall;
    public string? Band { get; set; } = log.ColBand;
    public string? Mode { get; set; } = log.ColMode;
    public string? SatName { get; set; } = log.ColSatName;
    public int PotaCount { get; set; } = log.PotaContacts?.Count ?? 0;
    public string? Metadata { get; set; }
}
