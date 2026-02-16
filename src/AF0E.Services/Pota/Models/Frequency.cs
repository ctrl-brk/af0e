using System.Text.Json.Serialization;

namespace AF0E.Services.Pota.Models;

/// <summary>
/// Represents a radio frequency in kHz
/// </summary>
[JsonConverter(typeof(FrequencyJsonConverter))]
public sealed record Frequency
{
    public string Value { get; }
    public decimal? ValueAsDecimal { get; }
    
    public Frequency(string? frequencyKhz)
    {
        Value = NormalizeFrequency(frequencyKhz);
        ValueAsDecimal = decimal.TryParse(frequencyKhz, out var freq) ? freq : null;
    }

    /// <summary>
    /// Gets the amateur radio band for this frequency (e.g., "20m", "40m")
    /// </summary>
    public string? Band => ValueAsDecimal switch
    {
        >= 1800 and < 2000 => "160m",
        >= 3500 and < 4000 => "80m",
        >= 5330 and < 5405 => "60m",
        >= 7000 and < 7300 => "40m",
        >= 10100 and < 10150 => "30m",
        >= 14000 and < 14350 => "20m",
        >= 18068 and < 18168 => "17m",
        >= 21000 and < 21450 => "15m",
        >= 24890 and < 24990 => "12m",
        >= 28000 and < 29700 => "10m",
        >= 50000 and < 54000 => "6m",
        >= 144000 and < 148000 => "2m",
        >= 222000 and < 225000 => "1.25m",
        >= 420000 and < 450000 => "70cm",
        _ => null
    };

    /// <summary>
    /// Checks if this frequency is within the specified band
    /// </summary>
    public bool IsOnBand(string band)
        => string.Equals(Band, band, StringComparison.OrdinalIgnoreCase);

    private static string NormalizeFrequency(string? frequencyKhz)
    {
        if (string.IsNullOrWhiteSpace(frequencyKhz))
            return string.Empty;

        // If it parses as decimal, format without unnecessary trailing zeros
        if (decimal.TryParse(frequencyKhz, out var freq))
            return freq.ToString("0.##");

        return frequencyKhz;
    }

    public override string ToString() => Value;

    // Implicit conversion from string
    public static implicit operator Frequency(string? value) => new(value);
    
    // Implicit conversion to string
    public static implicit operator string(Frequency frequency) => frequency.Value;
}
