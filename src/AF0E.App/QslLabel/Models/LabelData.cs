namespace QslLabel.Models;

internal sealed class LabelData
{
    public required string Call { get; init; }
    public string Delivery { get; set; } = string.Empty;
    public string[] QslComments { get; set; } = [];
    public required List<LogGridModel> Contacts { get; init; }
    public bool PrintHeader { get; set; } = true;
    public bool HasPota { get; set; }
    public int MaxCountyLength { get; set; }
    public bool ShortGrid { get; set; }
    public bool NoGrid { get; set; }
    public int TotalContacts { get; set; }
}
