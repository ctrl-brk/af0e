using System.Diagnostics.CodeAnalysis;

namespace AF0E.Common.Pota;

[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
public class PotaParkInfo
{
    public int ParkId { get; init; }
    public string Reference { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string Grid4 { get; set; } = null!;
    public string Grid6 { get; set; } = null!;
    public int ParktypeId { get; set; }
    public int Active { get; set; }
    public string ParkComments { get; set; } = null!;
    // ReSharper disable once InconsistentNaming
    public string ParkURLs { get; set; } = null!;
    public string Website { get; set; } = null!;
    public string ParktypeDesc { get; set; } = null!;
    public string LocationDesc { get; set; } = null!;
    public string LocationName { get; set; } = null!;
    public int EntityId { get; set; }
    public string EntityName { get; set; } = null!;
    public string ReferencePrefix { get; set; } = null!;
    public int EntityDeleted { get; set; }
    public string FirstActivator { get; set; } = null!;
    public string FirstActivationDate { get; set; } = null!;
}
