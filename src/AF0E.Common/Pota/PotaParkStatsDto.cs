using System.Diagnostics.CodeAnalysis;

namespace AF0E.Common.Pota;

[SuppressMessage("ReSharper", "PropertyCanBeMadeInitOnly.Global")]
public class PotaParkStats
{
    public string Reference { get; set; } = null!;
    public int Activations { get; set; }
    public int Attempts { get; set; }
    public int Contacts { get; set; }
}
