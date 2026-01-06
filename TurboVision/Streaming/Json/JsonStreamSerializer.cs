using System.Text.Json;
using System.Text.Json.Serialization;

namespace TurboVision.Streaming.Json;

/// <summary>
/// JSON serializer implementation using System.Text.Json.
/// Provides human-readable serialization format for TurboVision objects.
/// </summary>
public class JsonStreamSerializer : IStreamSerializer
{
    private readonly JsonSerializerOptions _options;
    private readonly StreamableTypeRegistry _registry;

    /// <summary>
    /// Gets the JSON serializer options used by this serializer.
    /// </summary>
    public JsonSerializerOptions Options => _options;

    /// <summary>
    /// Creates a new JSON stream serializer with default options.
    /// </summary>
    public JsonStreamSerializer() : this(StreamableTypeRegistry.Instance)
    {
    }

    /// <summary>
    /// Creates a new JSON stream serializer with a custom type registry.
    /// </summary>
    /// <param name="registry">The type registry to use.</param>
    public JsonStreamSerializer(StreamableTypeRegistry registry)
    {
        _registry = registry;
        _options = CreateDefaultOptions();
    }

    /// <summary>
    /// Creates the default JSON serializer options.
    /// </summary>
    private static JsonSerializerOptions CreateDefaultOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            NumberHandling = JsonNumberHandling.Strict,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        // Add custom converters for TurboVision types
        options.Converters.Add(new TPointJsonConverter());
        options.Converters.Add(new TRectJsonConverter());
        options.Converters.Add(new TKeyJsonConverter());
        options.Converters.Add(new TMenuJsonConverter());
        options.Converters.Add(new TMenuItemJsonConverter());
        options.Converters.Add(new TStatusItemJsonConverter());
        options.Converters.Add(new TStatusDefJsonConverter());

        return options;
    }

    /// <inheritdoc/>
    public void Write(Stream stream, IStreamable obj)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(obj);

        JsonSerializer.Serialize(stream, obj, obj.GetType(), _options);
    }

    /// <inheritdoc/>
    public T? Read<T>(Stream stream) where T : class, IStreamable
    {
        ArgumentNullException.ThrowIfNull(stream);

        var result = JsonSerializer.Deserialize<T>(stream, _options);

        // Reconstruct view hierarchy if needed
        if (result != null)
        {
            ViewHierarchyRebuilder.Rebuild(result);
        }

        return result;
    }

    /// <inheritdoc/>
    public void Register<T>(string typeId) where T : class, IStreamable, new()
    {
        _registry.Register<T>(typeId);
    }

    /// <summary>
    /// Serializes an object to a JSON string.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>The JSON string.</returns>
    public string Serialize(IStreamable obj)
    {
        ArgumentNullException.ThrowIfNull(obj);
        return JsonSerializer.Serialize(obj, obj.GetType(), _options);
    }

    /// <summary>
    /// Deserializes an object from a JSON string.
    /// </summary>
    /// <typeparam name="T">The expected type.</typeparam>
    /// <param name="json">The JSON string.</param>
    /// <returns>The deserialized object.</returns>
    public T? Deserialize<T>(string json) where T : class, IStreamable
    {
        ArgumentNullException.ThrowIfNull(json);

        var result = JsonSerializer.Deserialize<T>(json, _options);

        if (result != null)
        {
            ViewHierarchyRebuilder.Rebuild(result);
        }

        return result;
    }
}
