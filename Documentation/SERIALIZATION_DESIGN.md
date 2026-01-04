# Serialization Design

Interface-based serialization architecture for TurboVision streaming (Phase 14).

## Architecture

```
IStreamSerializer (interface)
├── BinarySerializer : IStreamSerializer  (C++ binary format compatible)
└── JsonSerializer : IStreamSerializer    (modern JSON via System.Text.Json)
```

## Interface Definition

```csharp
/// <summary>
/// Contract for serializing TurboVision objects.
/// </summary>
public interface IStreamSerializer
{
    /// <summary>
    /// Writes an object to a stream.
    /// </summary>
    void Write(Stream stream, TStreamable obj);

    /// <summary>
    /// Reads an object from a stream.
    /// </summary>
    T Read<T>(Stream stream) where T : TStreamable;

    /// <summary>
    /// Registers a type for polymorphic serialization.
    /// </summary>
    void Register<T>(string typeId) where T : TStreamable, new();
}

/// <summary>
/// Base interface for all streamable objects.
/// </summary>
public interface TStreamable
{
    /// <summary>
    /// Writes object state to the serializer.
    /// </summary>
    void Write(IStreamWriter writer);

    /// <summary>
    /// Reads object state from the serializer.
    /// </summary>
    void Read(IStreamReader reader);
}
```

## Implementations

### BinarySerializer

- Compatible with upstream C++ binary format
- Uses `BinaryReader`/`BinaryWriter`
- Type registration IDs match upstream constants
- Supports reading files created by original Turbo Vision

```csharp
public class BinarySerializer : IStreamSerializer
{
    private readonly Dictionary<string, Func<TStreamable>> _factories = new();
    private readonly Dictionary<Type, string> _typeIds = new();

    public void Register<T>(string typeId) where T : TStreamable, new()
    {
        _factories[typeId] = () => new T();
        _typeIds[typeof(T)] = typeId;
    }

    // Implementation reads/writes 2-byte type ID prefix
    // followed by object data in upstream binary layout
}
```

### JsonSerializer

- Modern JSON format via `System.Text.Json`
- Uses `[JsonPolymorphic]` for type discrimination
- Human-readable and debuggable
- No legacy file compatibility

```csharp
public class JsonSerializer : IStreamSerializer
{
    private readonly JsonSerializerOptions _options;

    public JsonSerializer()
    {
        _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    // Implementation uses $type discriminator for polymorphism
}
```

## Upstream Mapping

| Upstream Concept | Interface Method | Notes |
|-----------------|------------------|-------|
| `TStreamable::write()` | `TStreamable.Write(IStreamWriter)` | Instance method on objects |
| `TStreamable::read()` | `TStreamable.Read(IStreamReader)` | Instance method on objects |
| `TStreamableClass::build()` | Factory via `Register<T>()` | Type registration |
| `RegisterType()` macro | `IStreamSerializer.Register<T>()` | Manual registration |
| Stream type IDs | `typeId` parameter | String-based in C# |
| `ipstream`/`opstream` | `IStreamReader`/`IStreamWriter` | Reader/writer abstractions |

## Classes Requiring TStreamable

All view classes and collections need `TStreamable` implementation:

### Views (from Phase 4-7)
- `TView`, `TGroup`, `TWindow`, `TDialog`
- `TFrame`, `TScrollBar`, `TScroller`
- `TStaticText`, `TLabel`, `TButton`
- `TInputLine`, `TCheckBoxes`, `TRadioButtons`
- `TListViewer`, `TListBox`, `THistory`
- `TMenuBar`, `TMenuBox`, `TStatusLine`

### Controls (from Phase 6)
- `TCluster`, `TMultiCheckBoxes`
- `TParamText`

### Collections (Phase 9)
- `TCollection`, `TSortedCollection`
- `TStringCollection`, `TResourceCollection`

### Additional (Phase 10-13)
- `TEditor`, `TMemo`, `TFileEditor`
- `TOutlineViewer`, `TOutline`
- `TColorSelector`, `TColorDialog`
- `THelpViewer`, `THelpWindow`

## JSON Schema Example

```json
{
  "$type": "TDialog",
  "bounds": { "a": { "x": 10, "y": 5 }, "b": { "x": 50, "y": 20 } },
  "title": "Sample Dialog",
  "flags": 3,
  "subViews": [
    {
      "$type": "TButton",
      "bounds": { "a": { "x": 15, "y": 12 }, "b": { "x": 25, "y": 14 } },
      "title": "~O~K",
      "command": 10
    }
  ]
}
```

## Usage Example

```csharp
// Registration (typically at application startup)
var serializer = new JsonSerializer();
serializer.Register<TDialog>("TDialog");
serializer.Register<TButton>("TButton");
serializer.Register<TInputLine>("TInputLine");

// Saving
using var file = File.Create("dialog.json");
serializer.Write(file, myDialog);

// Loading
using var file = File.OpenRead("dialog.json");
var dialog = serializer.Read<TDialog>(file);
```

## Implementation Notes

1. **Polymorphism**: Both implementations handle polymorphic hierarchies differently
   - Binary: 2-byte type ID prefix (upstream compatible)
   - JSON: `$type` discriminator property

2. **Circular references**: View hierarchies have parent/child refs
   - Binary: Uses object IDs with fixup pass
   - JSON: Uses `[JsonIgnore]` on parent refs, reconstruct on load

3. **Default values**: Skip serializing default values to reduce size
   - JSON: `DefaultIgnoreCondition = WhenWritingDefault`
   - Binary: Always write (for upstream compatibility)

4. **Versioning**: JSON naturally supports adding new properties
   - Binary: May need version field for future extensions
