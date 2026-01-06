namespace TurboVision.Streaming;

/// <summary>
/// Maintains a registry of all streamable types for polymorphic serialization.
/// Singleton pattern matches upstream TStreamableTypes.
/// </summary>
public sealed class StreamableTypeRegistry
{
    private static readonly Lazy<StreamableTypeRegistry> _instance = new(() => new StreamableTypeRegistry());

    /// <summary>
    /// Gets the singleton instance of the type registry.
    /// </summary>
    public static StreamableTypeRegistry Instance => _instance.Value;

    private readonly Dictionary<string, Type> _nameToType = new();
    private readonly Dictionary<Type, string> _typeToName = new();
    private readonly Dictionary<string, Func<IStreamable>> _factories = new();

    private StreamableTypeRegistry()
    {
    }

    /// <summary>
    /// Registers a type with its identifier and factory function.
    /// </summary>
    /// <typeparam name="T">The type to register.</typeparam>
    /// <param name="typeId">The type identifier string (matches upstream name constant).</param>
    public void Register<T>(string typeId) where T : class, IStreamable, new()
    {
        var type = typeof(T);
        _nameToType[typeId] = type;
        _typeToName[type] = typeId;
        _factories[typeId] = () => new T();
    }

    /// <summary>
    /// Gets the Type for a given type identifier.
    /// </summary>
    /// <param name="typeId">The type identifier.</param>
    /// <returns>The Type, or null if not registered.</returns>
    public Type? GetType(string typeId)
    {
        return _nameToType.GetValueOrDefault(typeId);
    }

    /// <summary>
    /// Gets the type identifier for a given Type.
    /// </summary>
    /// <param name="type">The Type to look up.</param>
    /// <returns>The type identifier, or null if not registered.</returns>
    public string? GetTypeId(Type type)
    {
        return _typeToName.GetValueOrDefault(type);
    }

    /// <summary>
    /// Creates an instance of a type by its identifier.
    /// Matches upstream TStreamableClass::build().
    /// </summary>
    /// <param name="typeId">The type identifier.</param>
    /// <returns>A new instance, or null if not registered.</returns>
    public IStreamable? Create(string typeId)
    {
        return _factories.TryGetValue(typeId, out var factory) ? factory() : null;
    }

    /// <summary>
    /// Checks if a type identifier is registered.
    /// </summary>
    /// <param name="typeId">The type identifier.</param>
    /// <returns>True if registered.</returns>
    public bool IsRegistered(string typeId)
    {
        return _nameToType.ContainsKey(typeId);
    }

    /// <summary>
    /// Gets all registered type identifiers.
    /// </summary>
    public IEnumerable<string> RegisteredTypes => _nameToType.Keys;
}
