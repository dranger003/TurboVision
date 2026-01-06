# Serialization Design

Interface-based serialization architecture for TurboVision streaming.

**Status:** ✓ IMPLEMENTED (85% complete)

## Architecture

```
IStreamSerializer (interface)
├── JsonStreamSerializer : IStreamSerializer  ✓ IMPLEMENTED (modern JSON via System.Text.Json)
└── BinarySerializer : IStreamSerializer      ○ NOT IMPLEMENTED (upstream format compatible)
```

**Design Decision:** JSON-native approach was chosen for human-readability and debuggability. Binary format compatibility with upstream C++ is not required.

---

## Implementation Status

| Component | File | Status |
|-----------|------|--------|
| IStreamable | Streaming/IStreamable.cs | ✓ Complete |
| IStreamSerializer | Streaming/IStreamSerializer.cs | ✓ Complete |
| IStreamReader | Streaming/IStreamReader.cs | ✓ Complete |
| IStreamWriter | Streaming/IStreamWriter.cs | ✓ Complete |
| StreamableTypeRegistry | Streaming/StreamableTypeRegistry.cs | ✓ Complete |
| JsonStreamSerializer | Streaming/Json/JsonStreamSerializer.cs | ✓ Complete |
| ViewHierarchyRebuilder | Streaming/Json/ViewHierarchyRebuilder.cs | ✓ Complete |
| TPointJsonConverter | Streaming/Json/TPointJsonConverter.cs | ✓ Complete |
| TRectJsonConverter | Streaming/Json/TRectJsonConverter.cs | ✓ Complete |
| TKeyJsonConverter | Streaming/Json/TKeyJsonConverter.cs | ✓ Complete |
| TMenuJsonConverter | Streaming/Json/TMenuJsonConverter.cs | ✓ Complete |
| TStatusJsonConverters | Streaming/Json/TStatusJsonConverters.cs | ✓ Complete |

---

## Interface Definitions

```csharp
/// <summary>
/// Contract for serializing TurboVision objects.
/// </summary>
public interface IStreamSerializer
{
    /// <summary>
    /// Writes an object to a stream.
    /// </summary>
    void Write(Stream stream, IStreamable obj);

    /// <summary>
    /// Reads an object from a stream.
    /// </summary>
    T Read<T>(Stream stream) where T : IStreamable;

    /// <summary>
    /// Registers a type for polymorphic serialization.
    /// </summary>
    void Register<T>(string typeId) where T : IStreamable, new();
}

/// <summary>
/// Base interface for all streamable objects.
/// </summary>
public interface IStreamable
{
    /// <summary>
    /// Type identifier for serialization.
    /// </summary>
    string StreamableName { get; }
}
```

---

## JSON Implementation

### JsonStreamSerializer Features

- Human-readable indented JSON output
- Uses `System.Text.Json` with `[JsonPolymorphic]` attributes
- Type discrimination via `$type` property
- Custom converters for complex types (TPoint, TRect, TKey, TMenu, TStatusItem)
- State masking - runtime flags (sfActive, sfSelected) excluded from serialization
- View hierarchy pointer fixup via `ViewHierarchyRebuilder`

### Configuration

```csharp
public class JsonStreamSerializer : IStreamSerializer
{
    private readonly JsonSerializerOptions _options;

    public JsonStreamSerializer()
    {
        _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
        };

        // Add custom converters
        _options.Converters.Add(new TPointJsonConverter());
        _options.Converters.Add(new TRectJsonConverter());
        _options.Converters.Add(new TKeyJsonConverter());
        _options.Converters.Add(new TMenuJsonConverter());
        _options.Converters.Add(new TMenuItemJsonConverter());
        _options.Converters.Add(new TStatusItemJsonConverter());
        _options.Converters.Add(new TStatusDefJsonConverter());
    }
}
```

---

## View Types with JSON Serialization (30+ types)

### Base Views
- TView, TGroup, TFrame, TWindow, TDialog

### Controls
- TButton, TInputLine, TLabel, TStaticText, TParamText

### Clusters
- TCheckBoxes, TRadioButtons, TMultiCheckBoxes

### Lists
- TListBox, TSortedListBox, TListViewer

### Scrolling
- TScrollBar, TScroller

### History
- THistory, THistoryViewer, THistoryWindow

### File Dialogs
- TFileInputLine, TFileInfoPane, TFileList, TDirListBox, TFileDialog, TChDirDialog

### Menus
- TMenuView, TMenuBar, TMenuBox, TMenuPopup, TStatusLine

### Editor
- TEditor, TMemo, TFileEditor, TIndicator, TEditWindow

### Misc
- TBackground

---

## Polymorphic Serialization

View classes use `[JsonPolymorphic]` and `[JsonDerivedType]` attributes:

```csharp
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TView), "TView")]
[JsonDerivedType(typeof(TGroup), "TGroup")]
[JsonDerivedType(typeof(TFrame), "TFrame")]
[JsonDerivedType(typeof(TWindow), "TWindow")]
[JsonDerivedType(typeof(TDialog), "TDialog")]
[JsonDerivedType(typeof(TButton), "TButton")]
[JsonDerivedType(typeof(TInputLine), "TInputLine")]
// ... 30+ derived types
public class TView : TObject, IStreamable
{
    // ...
}
```

---

## View Hierarchy Reconstruction

The `ViewHierarchyRebuilder` handles pointer fixup after deserialization:

```csharp
public class ViewHierarchyRebuilder
{
    /// <summary>
    /// Reconstructs Owner, Next, and Last pointers after JSON deserialization.
    /// </summary>
    public void Rebuild(TGroup root)
    {
        // Walk the view tree and restore linked list pointers
        // Handle TLabel.Link, THistory.Link via LinkIndex
    }
}
```

### Linked View Resolution

For views that link to other views (TLabel → linked control, THistory → linked input):

1. During serialization: Store `LinkIndex` (index in parent's child list)
2. During deserialization: Use `ViewHierarchyRebuilder` to resolve index to actual view reference

---

## JSON Schema Example

```json
{
  "$type": "TDialog",
  "bounds": { "a": { "x": 10, "y": 5 }, "b": { "x": 50, "y": 20 } },
  "title": "Sample Dialog",
  "flags": 3,
  "options": 0,
  "growMode": 0,
  "subViews": [
    {
      "$type": "TButton",
      "bounds": { "a": { "x": 15, "y": 12 }, "b": { "x": 25, "y": 14 } },
      "title": "~O~K",
      "command": 10,
      "flags": 7
    },
    {
      "$type": "TInputLine",
      "bounds": { "a": { "x": 10, "y": 3 }, "b": { "x": 40, "y": 4 } },
      "maxLen": 128,
      "data": ""
    }
  ]
}
```

---

## Usage Example

```csharp
// Create serializer
var serializer = new JsonStreamSerializer();

// Saving a dialog
using var file = File.Create("dialog.json");
serializer.Write(file, myDialog);

// Loading a dialog
using var file = File.OpenRead("dialog.json");
var dialog = serializer.Read<TDialog>(file);

// Rebuild view hierarchy pointers
var rebuilder = new ViewHierarchyRebuilder();
rebuilder.Rebuild(dialog);
```

---

## State Masking

Runtime state flags are excluded from serialization using `[JsonIgnore]`:

```csharp
public class TView
{
    // Serialized
    public TRect Bounds { get; set; }
    public OptionFlags Options { get; set; }
    public GrowFlags GrowMode { get; set; }

    // NOT serialized (runtime state)
    [JsonIgnore]
    public StateFlags State { get; private set; }  // sfActive, sfSelected, sfFocused

    [JsonIgnore]
    public TGroup? Owner { get; internal set; }  // Reconstructed by ViewHierarchyRebuilder

    [JsonIgnore]
    public TView? Next { get; internal set; }  // Reconstructed by ViewHierarchyRebuilder
}
```

---

## Upstream Mapping

| Upstream Concept | C# Implementation | Notes |
|-----------------|-------------------|-------|
| `TStreamable::write()` | `IStreamable` + JSON attributes | Via System.Text.Json |
| `TStreamable::read()` | JSON deserialization | Via System.Text.Json |
| `TStreamableClass::build()` | `[JsonDerivedType]` | Automatic type handling |
| `RegisterType()` macro | `[JsonDerivedType]` attributes | Compile-time registration |
| Stream type IDs | `$type` discriminator | String-based |
| `ipstream`/`opstream` | `JsonStreamSerializer` | Single class handles both |
| Object tracking | `ViewHierarchyRebuilder` | Post-deserialization fixup |

---

## Not Implemented

| Feature | Status | Notes |
|---------|--------|-------|
| Binary format | ○ Not Implemented | Upstream C++ compatibility not required |
| Validator serialization | ○ Not Implemented | Validators are code, not data - intentional |
| TCollection<T> JSON converter | ○ Pending | For future collection serialization |

---

## Test Coverage

Serialization tests in `TurboVision.Tests/Streaming/JsonSerializerTests.cs`:

- Round-trip tests for all view types
- Hierarchy reconstruction tests
- Custom converter tests (TPoint, TRect, TKey)
- Menu/StatusLine serialization tests

**Total: 21+ serialization tests (all passing)**

---

*Tracking commit: 0707c8f*
