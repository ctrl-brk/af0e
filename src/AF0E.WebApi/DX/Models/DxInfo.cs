using AF0E.Common.Entities;
using System.Collections.ObjectModel;
using System.Text.Json;

namespace DX.Api.Models;

public class DxInfo
{
    public DxInfo(DxInfoTableEntity entity)
    {
        CallSign = entity.CallSign.Replace('|', '/');
        Name = entity.Name;
        DXCC = entity.DXCC;
        IOTA = entity.IOTA;
        BeginDate = entity.BeginDate;
        EndDate = entity.EndDate;
        Description = entity.Description;
        if (entity.Links != null)
            Links = JsonSerializer.Deserialize<Collection<string>>(entity.Links)!;
    }

    public string CallSign { get; }
    public string? Name { get; set; }
    public string? DXCC { get; set; }
    public string? IOTA { get; set; }
    public DateTime BeginDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Description { get; set; }
    public Collection<string> Links { get; } = [];
}
