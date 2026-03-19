using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AF0E.Services.Pota.Models;

namespace AF0E.Services.UnitTests.Pota.Models;

[SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores")]
public class FrequencyJsonConverterTests
{
    private readonly JsonSerializerOptions _options = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Theory]
    [InlineData("\"14074\"", "14074")]
    [InlineData("\"7074\"", "7074")]
    [InlineData("\"\"", "")]
    public void Read_StringValue_CreatesFrequency(string json, string expectedValue)
    {
        var frequency = JsonSerializer.Deserialize<Frequency>(json);

        Assert.NotNull(frequency);
        Assert.Equal(expectedValue, frequency.Value);
    }

    [Theory]
    [InlineData(14074, "14074")]
    [InlineData(7074, "7074")]
    [InlineData(0, "0")]
    public void Read_NumericValue_CreatesFrequency(int jsonValue, string expectedValue)
    {
        var json = jsonValue.ToString();
        var frequency = JsonSerializer.Deserialize<Frequency>(json);

        Assert.NotNull(frequency);
        Assert.Equal(expectedValue, frequency.Value);
    }

    [Fact]
    public void Read_NullValue_ReturnsNull()
    {
        var json = "null";
        var frequency = JsonSerializer.Deserialize<Frequency>(json);

        Assert.Null(frequency);
    }

    [Fact]
    public void Read_InvalidTokenType_ThrowsJsonException()
    {
        const string json = "{\"nested\": \"object\"}";

        var exception = Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Frequency>(json));

        Assert.Contains("Unable to convert token type", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("14074", "\"14074\"")]
    [InlineData("7074", "\"7074\"")]
    public void Write_FrequencyWithValue_WritesString(string value, string expectedJson)
    {
        var frequency = new Frequency(value);

        var json = JsonSerializer.Serialize(frequency);

        Assert.Equal(expectedJson, json);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Write_FrequencyWithNullOrEmpty_WritesNull(string? value)
    {
        var frequency = new Frequency(value);

        var json = JsonSerializer.Serialize(frequency);

        Assert.Equal("null", json);
    }

    [Fact]
    public void Deserialize_PotaSpotWithStringFrequency_Success()
    {
        var json = """
        {
            "spotId": 123,
            "activator": "KN4CRD",
            "frequency": "14074",
            "mode": "FT8",
            "reference": "K-1234",
            "spotTime": "2024-01-01T12:00:00Z",
            "spotter": "W1AW"
        }
        """;

        var spot = JsonSerializer.Deserialize<PotaSpot>(json, _options);

        Assert.NotNull(spot);
        Assert.Equal("14074", spot.Frequency.Value);
        Assert.Equal(14074m, spot.Frequency.ValueAsDecimal);
        Assert.Equal("20m", spot.Frequency.Band);
    }

    [Fact]
    public void Deserialize_PotaSpotWithNumericFrequency_Success()
    {
        var json = """
        {
            "spotId": 123,
            "activator": "KN4CRD",
            "frequency": 7074,
            "mode": "FT8",
            "reference": "K-1234",
            "spotTime": "2024-01-01T12:00:00Z",
            "spotter": "W1AW"
        }
        """;

        var spot = JsonSerializer.Deserialize<PotaSpot>(json, _options);

        Assert.NotNull(spot);
        Assert.Equal("7074", spot.Frequency.Value);
        Assert.Equal(7074m, spot.Frequency.ValueAsDecimal);
        Assert.Equal("40m", spot.Frequency.Band);
    }

    [Fact]
    public void Deserialize_PotaSpotArray_Success()
    {
        var json = """
        [
            {
                "spotId": 1,
                "activator": "KN4CRD",
                "frequency": "14074",
                "mode": "FT8",
                "reference": "K-1234",
                "spotTime": "2024-01-01T12:00:00Z",
                "spotter": "W1AW"
            },
            {
                "spotId": 2,
                "activator": "W1AW",
                "frequency": 7074,
                "mode": "SSB",
                "reference": "K-5678",
                "spotTime": "2024-01-01T13:00:00Z",
                "spotter": "KN4CRD"
            }
        ]
        """;

        var spots = JsonSerializer.Deserialize<List<PotaSpot>>(json, _options);

        Assert.NotNull(spots);
        Assert.Equal(2, spots.Count);
        Assert.Equal("14074", spots[0].Frequency.Value);
        Assert.Equal("7074", spots[1].Frequency.Value);
    }

    [Fact]
    public void RoundTrip_Frequency_PreservesValue()
    {
        var original = new Frequency("14074");

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<Frequency>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Value, deserialized.Value);
        Assert.Equal(original.ValueAsDecimal, deserialized.ValueAsDecimal);
        Assert.Equal(original.Band, deserialized.Band);
    }
}
