using AF0E.DB.Models;

namespace Logbook.Api.Models;

public sealed class HamEventSummary(HamEvent hamEvent)
{
    public int Id { get; } = hamEvent.HamEventId;
    public string EventType { get; } = hamEvent.EventType;
    public string Name { get; } = hamEvent.Name;
    public DateTime? StartDate { get; } = hamEvent.StartDate;
    public DateTime? EndDate { get; } = hamEvent.EndDate;
}
