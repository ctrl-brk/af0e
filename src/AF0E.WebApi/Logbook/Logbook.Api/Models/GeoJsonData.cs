using System.Diagnostics.CodeAnalysis;

namespace Logbook.Api.Models;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public sealed class GeoJsonData
{
    public required string Type { get; init; }
    public required IEnumerable<object> Features { get; init; }
}
