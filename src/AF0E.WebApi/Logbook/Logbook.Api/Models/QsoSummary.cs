using System.Diagnostics.CodeAnalysis;
using AF0E.DB.Models;

namespace Logbook.Api.Models;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public sealed class QsoSummary(HrdLog log, bool isAdmin = false)
{
    public QsoSummary(HrdLog log, string metadata) : this(log)
    {
        Metadata = metadata;
    }
    public int Id { get; } = log.ColPrimaryKey;
    public DateTime Date { get; } = log.ColTimeOn!.Value;
    public string Call { get; } = log.ColCall;
    public string? Band { get; } = log.ColBand;
    public string? Mode { get; } = log.ColMode;
    public string? StationCallsign { get; } = log.ColStationCallsign;
    public string? OperatorCallsign { get; } = log.ColOperator;
    public string? Comment { get; } = isAdmin ? log.ColComment : null;
    public string? SatName { get; } = log.ColSatName;
    public int PotaCount { get; } = log.PotaContacts.Count;
    public string? Metadata { get; }
}
