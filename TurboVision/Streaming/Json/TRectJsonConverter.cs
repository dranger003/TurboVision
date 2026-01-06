using System.Text.Json;
using System.Text.Json.Serialization;
using TurboVision.Core;

namespace TurboVision.Streaming.Json;

/// <summary>
/// Custom JSON converter for TRect.
/// Serializes as {"a": {"x": N, "y": N}, "b": {"x": N, "y": N}}.
/// </summary>
public sealed class TRectJsonConverter : JsonConverter<TRect>
{
    private readonly TPointJsonConverter _pointConverter = new();

    public override TRect Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token for TRect");
        }

        TPoint a = default;
        TPoint b = default;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return new TRect(a, b);
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected PropertyName token");
            }

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName?.ToLowerInvariant())
            {
                case "a":
                    a = _pointConverter.Read(ref reader, typeof(TPoint), options);
                    break;
                case "b":
                    b = _pointConverter.Read(ref reader, typeof(TPoint), options);
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        throw new JsonException("Unexpected end of JSON");
    }

    public override void Write(Utf8JsonWriter writer, TRect value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("a");
        _pointConverter.Write(writer, value.A, options);
        writer.WritePropertyName("b");
        _pointConverter.Write(writer, value.B, options);
        writer.WriteEndObject();
    }
}
