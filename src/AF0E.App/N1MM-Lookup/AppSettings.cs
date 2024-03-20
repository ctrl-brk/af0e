namespace N1MMLookup;

public class AppSettings
{
    public int UdpPort { get; init; } = 12060;
#pragma warning disable CA1056
    public string? QrzApiUrl { get; set; } = "https://xmldata.qrz.com/xml/current";
#pragma warning restore CA1056
    public string? QrzUser { get; set; }
    public string? QrzPassword { get; set;}
}
