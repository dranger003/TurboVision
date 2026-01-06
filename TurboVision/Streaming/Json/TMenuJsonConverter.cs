using System.Text.Json;
using System.Text.Json.Serialization;
using TurboVision.Core;
using TurboVision.Menus;
using TurboVision.Views;

namespace TurboVision.Streaming.Json;

/// <summary>
/// JSON converter for TMenu.
/// Serializes menu items as an array instead of linked list.
/// </summary>
public class TMenuJsonConverter : JsonConverter<TMenu>
{
    public override TMenu? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token for TMenu");
        }

        List<TMenuItem>? items = null;
        int defaultIndex = -1;

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
                    case "items":
                        items = JsonSerializer.Deserialize<List<TMenuItem>>(ref reader, options);
                        break;
                    case "defaultIndex":
                        defaultIndex = reader.GetInt32();
                        break;
                }
            }
        }

        var menu = new TMenu();

        // Rebuild linked list from array
        if (items != null && items.Count > 0)
        {
            for (int i = 0; i < items.Count - 1; i++)
            {
                items[i].Next = items[i + 1];
            }
            menu.Items = items[0];
            menu.Default = defaultIndex >= 0 && defaultIndex < items.Count ? items[defaultIndex] : menu.Items;
        }

        return menu;
    }

    public override void Write(Utf8JsonWriter writer, TMenu value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        // Convert linked list to array
        var items = new List<TMenuItem>();
        int defaultIndex = -1;
        int index = 0;

        for (var item = value.Items; item != null; item = item.Next)
        {
            if (item == value.Default)
            {
                defaultIndex = index;
            }
            items.Add(item);
            index++;
        }

        writer.WritePropertyName("items");
        JsonSerializer.Serialize(writer, items, options);

        if (defaultIndex >= 0)
        {
            writer.WriteNumber("defaultIndex", defaultIndex);
        }

        writer.WriteEndObject();
    }
}

/// <summary>
/// JSON converter for TMenuItem.
/// Handles the union of Param/SubMenu and ignores Next (serialized as array in TMenu).
/// </summary>
public class TMenuItemJsonConverter : JsonConverter<TMenuItem>
{
    public override TMenuItem? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected StartObject token for TMenuItem");
        }

        string? name = null;
        ushort command = 0;
        TKey keyCode = default;
        ushort helpCtx = 0;
        string? param = null;
        TMenu? subMenu = null;

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
                    case "name":
                        name = reader.GetString();
                        break;
                    case "command":
                        command = reader.GetUInt16();
                        break;
                    case "keyCode":
                        keyCode = JsonSerializer.Deserialize<TKey>(ref reader, options);
                        break;
                    case "helpCtx":
                        helpCtx = reader.GetUInt16();
                        break;
                    case "param":
                        param = reader.GetString();
                        break;
                    case "subMenu":
                        subMenu = JsonSerializer.Deserialize<TMenu>(ref reader, options);
                        break;
                }
            }
        }

        // Create appropriate item type based on subMenu presence
        if (subMenu != null)
        {
            return new TMenuItem(name, keyCode, subMenu, helpCtx);
        }
        else
        {
            return new TMenuItem(name, command, keyCode, helpCtx, param);
        }
    }

    public override void Write(Utf8JsonWriter writer, TMenuItem value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        if (value.Name != null)
        {
            writer.WriteString("name", value.Name);
        }

        writer.WriteNumber("command", value.Command);

        writer.WritePropertyName("keyCode");
        JsonSerializer.Serialize(writer, value.KeyCode, options);

        if (value.HelpCtx != HelpContexts.hcNoContext)
        {
            writer.WriteNumber("helpCtx", value.HelpCtx);
        }

        if (value.SubMenu != null)
        {
            writer.WritePropertyName("subMenu");
            JsonSerializer.Serialize(writer, value.SubMenu, options);
        }
        else if (value.Param != null)
        {
            writer.WriteString("param", value.Param);
        }

        writer.WriteEndObject();
    }
}
