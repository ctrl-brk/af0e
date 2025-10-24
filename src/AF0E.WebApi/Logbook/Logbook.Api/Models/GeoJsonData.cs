namespace Logbook.Api.Models;

public sealed class GeoJsonData
{
    public string Type { get; set; } = null!;
    public IEnumerable<object> Features { get; set; } = [];
}
