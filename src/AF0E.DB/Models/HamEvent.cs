using System.Diagnostics.CodeAnalysis;

namespace AF0E.DB.Models;

[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
public sealed class HamEvent
{
    public int HamEventId { get; init; }
    /// <summary>
    /// C - contest, E - special event, D - DXpedition
    /// </summary>
    public string EventType { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Uri? Url { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public DateTime? LogDueDate { get; set; }
    public DateTime? QslDueDate { get; set; }
    public string? QslInfo { get; set; }
    public string? Comments { get; set; }

    public ICollection<HamEventContact> HamEventContacts { get; init; } = new List<HamEventContact>();
}
