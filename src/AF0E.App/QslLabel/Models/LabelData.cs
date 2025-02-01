namespace QslLabel.Models;

internal sealed class LabelData
{
    public required string Call { get; init; }
    public string? Delivery { get; init; }
    public string[] QslComments { get; set; } = [];
    public required List<LogGridModel> Contacts { get; init; }
}
