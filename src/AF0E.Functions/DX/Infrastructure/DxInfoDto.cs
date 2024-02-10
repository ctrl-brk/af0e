using System.Text.Json;
using Azure;
using Azure.Data.Tables;

namespace AF0E.Functions.DX.Infrastructure;

public sealed class DxInfoDto : ITableEntity
{
    public DxInfoDto(string partitionKey, DxInfo info)
    {
        Name = info.Name;
        DXCC = info.DXCC;
        IOTA = info.IOTA;
        CallSign = info.CallSign;

        BeginDate = info.BeginDate;
        if (BeginDate.Kind == DateTimeKind.Unspecified)
            BeginDate = DateTime.SpecifyKind(BeginDate, DateTimeKind.Utc);

        BeginDateSet = info.BeginDateSet;

        EndDate = info.EndDate;
        if (EndDate.Kind == DateTimeKind.Unspecified)
            EndDate = DateTime.SpecifyKind(EndDate, DateTimeKind.Utc);

        EndDateSet = info.EndDateSet;

        Links = JsonSerializer.Serialize(info.Links);
        Description = info.Description;

        PartitionKey = partitionKey;
        RowKey = info.CallSign.Replace('/', '|');
    }

#pragma warning disable CS8618
    // ReSharper disable UnusedMember.Global
    /// <summary>
    /// Json constructor
    /// </summary>
    public DxInfoDto() {}
    // ReSharper restore UnusedMember.Global
#pragma warning restore CS8618

    // ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
    public string? Name { get; set; }
    public string? DXCC { get; set; }
    public string? IOTA { get; set; }
    public string CallSign { get; set; } //set is for json only
    public DateTime BeginDate { get; set; }
    public bool BeginDateSet { get; set; }
    public DateTime EndDate { get; set; }
    public bool EndDateSet { get; set; }
    public string? Links { get; set; } //set is for json only
    public string? Description { get; set; }

    public string PartitionKey { get; set; } = null!;
    public string RowKey { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
    // ReSharper restore AutoPropertyCanBeMadeGetOnly.Global
}
