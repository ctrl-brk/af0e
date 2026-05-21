using System.Text.Json.Serialization;
using AF0E.Common.Radio;

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
    public string? Band => RadioHelper.DetectBand(ValueAsDecimal);

    public static string? DetectBand(decimal? frequencyKhz)
        => RadioHelper.DetectBand(frequencyKhz);

    public static string? NormalizeBand(string? value)
        => RadioHelper.NormalizeBand(value);

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
        return decimal.TryParse(frequencyKhz, out var freq) ? freq.ToString("0.##") : frequencyKhz;
    }

    public override string ToString() => Value;

    // Implicit conversion from string
#pragma warning disable CA2225
    public static implicit operator Frequency(string? value) => new(value);
#pragma warning restore CA2225

    // Implicit conversion to string
    public static implicit operator string(Frequency frequency) => frequency.Value;
}
