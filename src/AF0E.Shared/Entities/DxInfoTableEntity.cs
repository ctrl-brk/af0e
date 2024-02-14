using Azure;
using Azure.Data.Tables;
// ReSharper disable PropertyCanBeMadeInitOnly.Global
// ReSharper disable MemberCanBeProtected.Global

namespace AF0E.Shared.Entities;

public class DxInfoTableEntity : ITableEntity
{
    public string? Name { get; set; }
    public string? DXCC { get; set; }
    public string? IOTA { get; set; }
    public string CallSign { get; set; } = null!; //set is for json only
    public DateTime BeginDate { get; set; }
    public bool BeginDateSet { get; set; }
    public DateTime EndDate { get; set; }
    public bool EndDateSet { get; set; }
    public string? Links { get; set; } //set is for json only
    public string? Description { get; set; }

    public string PartitionKey { get; set; } = null!;
    public string RowKey { get; set; } = null!;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }
}
