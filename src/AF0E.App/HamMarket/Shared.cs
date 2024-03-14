namespace HamMarket;

public enum ScanType
{
    Keyword,
    Category
}

public class ScanInfo
{
    public DateTime Date { get; set; }
    public List<int> Ids { get; set; }
    [NonSerialized]
    public List<int> OtherIds;
}
