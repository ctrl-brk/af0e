using System.Text.Json;
using System.Text.Json.Serialization;

namespace AF0E.Services.Pota.Models;

public sealed class FrequencyJsonConverter : JsonConverter<Frequency>
{
    public override Frequency Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.TokenType switch
        {
            JsonTokenType.Null => new Frequency(null),
            JsonTokenType.String => new Frequency(reader.GetString()),
            JsonTokenType.Number => new Frequency(reader.GetDecimal().ToString("F2")),
            _ => throw new JsonException($"Unable to convert token type {reader.TokenType} to Frequency.")
        };
    }

    public override void Write(Utf8JsonWriter writer, Frequency value, JsonSerializerOptions options)
    {
        if (string.IsNullOrEmpty(value.Value))
            writer.WriteNullValue();
        else
            writer.WriteStringValue(value.Value);
    }
}
