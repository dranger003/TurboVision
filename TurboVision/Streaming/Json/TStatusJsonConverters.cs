using System.Text.Json;
using System.Text.Json.Serialization;
using TurboVision.Core;
using TurboVision.Menus;

namespace TurboVision.Streaming.Json;

/// <summary>
/// JSON converter for TStatusItem.
/// Ignores Next (serialized as array in TStatusDef).
/// </summary>
public class TStatusItemJsonConverter : JsonConverter<TStatusItem>
{
    public override TStatusItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token for TStatusItem");
        }

        string? text = null;
        TKey keyCode = default;
        ushort command = 0;

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
                    case "text":
                        text = reader.GetString();
                        break;
                    case "keyCode":
                        keyCode = JsonSerializer.Deserialize<TKey>(ref reader, options);
                        break;
                    case "command":
                        command = reader.GetUInt16();
                        break;
                }
            }
        }

        return new TStatusItem(text, keyCode, command);
    }

    public override void Write(Utf8JsonWriter writer, TStatusItem value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value.Text != null)
        {
            writer.WriteString("text", value.Text);
        }

        writer.WritePropertyName("keyCode");
        JsonSerializer.Serialize(writer, value.KeyCode, options);

        writer.WriteNumber("command", value.Command);

        writer.WriteEndObject();
    }
}

/// <summary>
/// JSON converter for TStatusDef.
/// Serializes Items as array and handles Next chain as array.
/// </summary>
public class TStatusDefJsonConverter : JsonConverter<TStatusDef>
{
    public override TStatusDef? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token for TStatusDef");
        }

        ushort min = 0;
        ushort max = 0;
        List<TStatusItem>? items = null;
        TStatusDef? next = null;

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
                    case "min":
                        min = reader.GetUInt16();
                        break;
                    case "max":
                        max = reader.GetUInt16();
                        break;
                    case "items":
                        items = JsonSerializer.Deserialize<List<TStatusItem>>(ref reader, options);
                        break;
                    case "next":
                        next = JsonSerializer.Deserialize<TStatusDef>(ref reader, options);
                        break;
                }
            }
        }

        // Rebuild status items linked list
        TStatusItem? firstItem = null;
        if (items != null && items.Count > 0)
        {
            for (int i = 0; i < items.Count - 1; i++)
            {
                items[i].Next = items[i + 1];
            }
            firstItem = items[0];
        }

        var def = new TStatusDef(min, max, firstItem, next);
        return def;
    }

    public override void Write(Utf8JsonWriter writer, TStatusDef value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WriteNumber("min", value.Min);
        writer.WriteNumber("max", value.Max);

        // Convert items linked list to array
        var items = new List<TStatusItem>();
        for (var item = value.Items; item != null; item = item.Next)
        {
            items.Add(item);
        }

        if (items.Count > 0)
        {
            writer.WritePropertyName("items");
            JsonSerializer.Serialize(writer, items, options);
        }

        if (value.Next != null)
        {
            writer.WritePropertyName("next");
            JsonSerializer.Serialize(writer, value.Next, options);
        }

        writer.WriteEndObject();
    }
}
