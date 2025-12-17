using System.Text.Json;
using System.Text.Json.Serialization;

namespace Logbook.Api.Converters;

/// <summary>
/// JSON converter factory that treats empty strings as null for all nullable numeric types
/// </summary>
public class NullableNumericConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        // Check if it's a nullable numeric type
        if (!typeToConvert.IsGenericType || typeToConvert.GetGenericTypeDefinition() != typeof(Nullable<>))
            return false;

        var underlyingType = Nullable.GetUnderlyingType(typeToConvert);
        return underlyingType == typeof(int) ||
               underlyingType == typeof(long) ||
               underlyingType == typeof(short) ||
               underlyingType == typeof(byte) ||
               underlyingType == typeof(double) ||
               underlyingType == typeof(float) ||
               underlyingType == typeof(decimal);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var underlyingType = Nullable.GetUnderlyingType(typeToConvert)!;

        return (JsonConverter)Activator.CreateInstance(
            typeof(NullableNumericConverter<>).MakeGenericType(underlyingType))!;
    }

    private class NullableNumericConverter<T> : JsonConverter<T?> where T : struct
    {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                {
                    var stringValue = reader.GetString();

                    // Empty string becomes null
                    if (string.IsNullOrWhiteSpace(stringValue))
                        return null;

                    // Try to parse the string to the target numeric type
                    try
                    {
                        return (T?)Convert.ChangeType(stringValue, typeof(T));
                    }
                    catch
                    {
                        // If parsing fails, return null
                        return null;
                    }
                }
                case JsonTokenType.Null:
                    return null;
                case JsonTokenType.Number:
                    return typeof(T) switch
                    {
                        { } t when t == typeof(int) => (T?)(object)reader.GetInt32(),
                        { } t when t == typeof(long) => (T?)(object)reader.GetInt64(),
                        { } t when t == typeof(short) => (T?)(object)(short)reader.GetInt32(),
                        { } t when t == typeof(byte) => (T?)(object)reader.GetByte(),
                        { } t when t == typeof(double) => (T?)(object)reader.GetDouble(),
                        { } t when t == typeof(float) => (T?)(object)reader.GetSingle(),
                        { } t when t == typeof(decimal) => (T?)(object)reader.GetDecimal(),
                        _ => throw new JsonException($"Unsupported numeric type: {typeof(T)}")
                    };
                default:
                    throw new JsonException($"Cannot convert {reader.TokenType} to nullable {typeof(T).Name}");
            }
        }

        public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }

            switch (value.Value)
            {
                case int i:
                    writer.WriteNumberValue(i);
                    break;
                case long l:
                    writer.WriteNumberValue(l);
                    break;
                case short s:
                    writer.WriteNumberValue(s);
                    break;
                case byte b:
                    writer.WriteNumberValue(b);
                    break;
                case double d:
                    writer.WriteNumberValue(d);
                    break;
                case float f:
                    writer.WriteNumberValue(f);
                    break;
                case decimal m:
                    writer.WriteNumberValue(m);
                    break;
                default:
                    throw new JsonException($"Unsupported numeric type: {typeof(T)}");
            }
        }
    }
}
