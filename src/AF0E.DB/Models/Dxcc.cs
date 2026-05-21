namespace AF0E.DB.Models;

public class Dxcc
{
    public string? Prefix { get; set; }
    public string EntityName { get; set; } = "";
    public string? PrefixRegExp { get; set; }
    public int EntityCode { get; set; }
    public string Deleted { get; set; } = "";
    public string CountryCode { get; set; } = "";
    public string DisplayName { get; set; } = "";
}
