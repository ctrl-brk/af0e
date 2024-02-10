using System.Collections.ObjectModel;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AF0E.Functions.DX.Infrastructure;

public sealed class DxInfo
{
    public DxInfo(DxInfoSource source, string callSign)
    {
        Source = source;
        CallSign = callSign;
        BeginDateSet = true;
        EndDateSet = true;
    }

    public DxInfo(DxInfoDto dto)
    {
        Source = DxInfoSource.Storage;
        CallSign = dto.CallSign.Replace('|', '/');

        Name = dto.Name;
        DXCC = dto.DXCC;
        IOTA = dto.IOTA;
        BeginDate = dto.BeginDate;
        BeginDateSet = dto.BeginDateSet;
        EndDate = dto.EndDate;
        EndDateSet = dto.EndDateSet;
        Description = dto.Description;

        if (dto.Links != null)
            Links = JsonSerializer.Deserialize<Collection<string>>(dto.Links)!;
    }

    /// <summary>
    /// Json constructor
    /// </summary>
    [JsonConstructor]
    public DxInfo(DxInfoSource source, string callSign, string? name, string? dxcc, string? iota, DateTime beginDate, bool beginDateSet, DateTime endDate, bool endDateSet, Collection<string> links, string? description)
    {
        Source = source;
        CallSign = callSign;
        Name = name;
        DXCC = dxcc;
        IOTA = iota;
        BeginDate = beginDate;
        BeginDateSet = beginDateSet;
        EndDate = endDate;
        EndDateSet = endDateSet;
        Links = links;
        Description = description;
    }

    public DxInfoSource Source { get; }
    public string CallSign { get; }
    public string? Name { get; set; }
    public string? DXCC { get; set; }
    public string? IOTA { get; set; }
    public DateTime BeginDate { get; set; }
    public bool BeginDateSet { get; set; }
    public DateTime EndDate { get; set; }
    public bool EndDateSet { get; set; }
    public Collection<string> Links { get; } = new();
    public string? Description { get; set; }

    public static IEnumerable<DxInfo> FromMultipleCallsigns(DxInfoSource source, string callSigns)
    {
        //ex: 'J79AN, J79BH', 'A31DL & A31DK'
        return callSigns.Replace('&', ',').Split(',').Where(x => !string.IsNullOrEmpty(x)).Select(callSign => new DxInfo(source, callSign.Trim()));
    }
}
