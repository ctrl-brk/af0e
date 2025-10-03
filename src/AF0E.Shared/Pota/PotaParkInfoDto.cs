namespace AF0E.Shared.Pota;

public class PotaParkInfo
{
    public int ParkId { get; set; }
    public string Reference { get; set; }
    public string Name { get; set; }
    public decimal Latitude { get; set; }
    public decimal Longitude { get; set; }
    public string Grid4 { get; set; }
    public string Grid6 { get; set; }
    public int ParktypeId { get; set; }
    public int Active { get; set; }
    public string ParkComments { get; set; }
    public List<string> ParkURLs { get; set; }
    public string Website { get; set; }
    public string ParktypeDesc { get; set; }
    public string LocationDesc { get; set; }
    public string LocationName { get; set; }
    public int EntityId { get; set; }
    public string EntityName { get; set; }
    public string ReferencePrefix { get; set; }
    public int EntityDeleted { get; set; }
    public string FirstActivator { get; set; }
    public string FirstActivationDate { get; set; }
}
