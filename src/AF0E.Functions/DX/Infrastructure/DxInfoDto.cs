using System.Text.Json;
using AF0E.Common.Entities;

namespace AF0E.Functions.DX.Infrastructure;

public sealed class DxInfoDto : DxInfoTableEntity
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

    // ReSharper disable once UnusedMember.Global
    /// <summary>
    /// Json constructor
    /// </summary>
    public DxInfoDto() {}
}
