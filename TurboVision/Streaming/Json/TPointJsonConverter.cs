using System.Text.Json;
using System.Text.Json.Serialization;
using TurboVision.Core;

namespace TurboVision.Streaming.Json;

/// <summary>
/// Custom JSON converter for TPoint.
/// Serializes as {"x": N, "y": N}.
/// </summary>
public sealed class TPointJsonConverter : JsonConverter<TPoint>
{
    public override TPoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token for TPoint");
        }

        int x = 0;
        int y = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return new TPoint(x, y);
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected PropertyName token");
            }

            var propertyName = reader.GetString();
            reader.Read();

            switch (propertyName?.ToLowerInvariant())
            {
                case "x":
                    x = reader.GetInt32();
                    break;
                case "y":
                    y = reader.GetInt32();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        throw new JsonException("Unexpected end of JSON");
    }

    public override void Write(Utf8JsonWriter writer, TPoint value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("x", value.X);
        writer.WriteNumber("y", value.Y);
        writer.WriteEndObject();
    }
}
