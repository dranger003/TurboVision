using System.Text.Json;
using System.Text.Json.Serialization;
using TurboVision.Core;

namespace TurboVision.Streaming.Json;

/// <summary>
/// JSON converter for TKey struct.
/// Serializes as {"keyCode": N, "controlKeyState": N}.
/// </summary>
public class TKeyJsonConverter : JsonConverter<TKey>
{
    public override TKey Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token for TKey");
        }

        ushort keyCode = 0;
        ushort controlKeyState = 0;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                break;
            }

            if (reader.TokenType == JsonTokenType.PropertyName)
            {
                string? propertyName = reader.GetString();
                reader.Read();

                switch (propertyName)
                {
                    case "keyCode":
                        keyCode = reader.GetUInt16();
                        break;
                    case "controlKeyState":
                        controlKeyState = reader.GetUInt16();
                        break;
                }
            }
        }

        return new TKey(keyCode, controlKeyState);
    }

    public override void Write(Utf8JsonWriter writer, TKey value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("keyCode", value.KeyCode);
        writer.WriteNumber("controlKeyState", value.ControlKeyState);
        writer.WriteEndObject();
    }
}
