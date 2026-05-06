using System.Diagnostics.CodeAnalysis;

namespace PotaParksImport;

// ReSharper disable once ClassNeverInstantiated.Global
[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public sealed class PotaParkImportRow
{
    public string? Reference { get; init; }

    public string? Name { get; init; }

    public decimal? Latitude { get; init; }

    public decimal? Longitude { get; init; }

    public string? Grid { get; init; }

    public string? LocationDesc { get; init; }

    public int? Attempts { get; init; }

    public int? Activations { get; init; }

    public int? Qsos { get; init; }
}
