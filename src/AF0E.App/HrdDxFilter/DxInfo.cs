using System.Collections.ObjectModel;

namespace HrdDxFilter;

// ReSharper disable once ClassNeverInstantiated.Global
public sealed class DxInfo
{
    public string CallSign { get; set; } = null!;
    public string? Name { get; set; }
    public string? DXCC { get; set; }
    public string? IOTA { get; set; }
    public DateTime BeginDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Description { get; set; }
#pragma warning disable CA2227
    public Collection<string> Links { get; set; } = [];
#pragma warning restore CA2227
}
